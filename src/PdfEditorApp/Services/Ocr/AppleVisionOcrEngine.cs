using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Services.Ocr;

/// <summary>
/// Ultra-fast hardware-accelerated OCR engine for macOS powered by Apple's Vision Framework
/// (VNRecognizeTextRequest / Apple Neural Engine / GPU). Requires zero downloads or external packages.
/// </summary>
public class AppleVisionOcrEngine : IOcrEngine
{
    public string EngineName => "Apple Vision OCR (macOS)";
    public OcrEngineType EngineType => OcrEngineType.OsNative;
    public bool IsAvailable => OperatingSystem.IsMacOS();

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    public Task<OcrResult> RecognizeTextAsync(byte[] imageBytes, string language = "eng", CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return Task.FromResult(new OcrResult
            {
                Success = false,
                ErrorMessage = "Apple Vision OCR is only supported on macOS."
            });
        }

        return Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = RunVisionOcr(imageBytes, language, ct);
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
                result.EngineUsed = EngineName;
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"Apple Vision OCR failed: {ex.Message}",
                    DurationMs = sw.ElapsedMilliseconds,
                    EngineUsed = EngineName
                };
            }
        }, ct);
    }

    private static OcrResult RunVisionOcr(byte[] imageBytes, string language, CancellationToken ct)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return new OcrResult { Success = false, ErrorMessage = "Empty image bytes." };
        }

        // Initialize frameworks
        NativeLibrary.Load("/System/Library/Frameworks/Foundation.framework/Foundation");
        NativeLibrary.Load("/System/Library/Frameworks/Vision.framework/Vision");

        IntPtr autoreleasePool = objc_msgSend(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc"));
        autoreleasePool = objc_msgSend(autoreleasePool, sel_registerName("init"));

        try
        {
            // Create NSData from image bytes
            IntPtr nsData = IntPtr.Zero;
            var handle = GCHandle.Alloc(imageBytes, GCHandleType.Pinned);
            try
            {
                IntPtr pBytes = handle.AddrOfPinnedObject();
                nsData = objc_msgSend_IntPtr_ulong(
                    objc_getClass("NSData"),
                    sel_registerName("dataWithBytes:length:"),
                    pBytes,
                    (ulong)imageBytes.Length);
            }
            finally
            {
                handle.Free();
            }

            if (nsData == IntPtr.Zero)
            {
                return new OcrResult { Success = false, ErrorMessage = "Failed to allocate NSData for image." };
            }

            // Create VNRecognizeTextRequest
            IntPtr request = objc_msgSend(objc_getClass("VNRecognizeTextRequest"), sel_registerName("alloc"));
            request = objc_msgSend(request, sel_registerName("init"));

            // Recognition level: 1 = Accurate, 0 = Fast
            objc_msgSend_void_long(request, sel_registerName("setRecognitionLevel:"), 1);
            objc_msgSend_void_bool(request, sel_registerName("setUsesLanguageCorrection:"), true);

            // Create VNImageRequestHandler with NSData and options
            IntPtr emptyDict = objc_msgSend(objc_getClass("NSDictionary"), sel_registerName("dictionary"));
            IntPtr handler = objc_msgSend(objc_getClass("VNImageRequestHandler"), sel_registerName("alloc"));
            handler = objc_msgSend_IntPtr_IntPtr(handler, sel_registerName("initWithData:options:"), nsData, emptyDict);

            // Create NSArray with [request]
            IntPtr reqArray = objc_msgSend_IntPtr(objc_getClass("NSArray"), sel_registerName("arrayWithObject:"), request);

            // Perform requests
            IntPtr pError = IntPtr.Zero;
            bool success = objc_msgSend_bool_IntPtr_outIntPtr(handler, sel_registerName("performRequests:error:"), reqArray, out pError);

            if (!success)
            {
                return new OcrResult { Success = false, ErrorMessage = "Vision request execution returned false." };
            }

            ct.ThrowIfCancellationRequested();

            // Read results: NSArray<VNRecognizedTextObservation>
            IntPtr resultsArray = objc_msgSend(request, sel_registerName("results"));
            ulong count = resultsArray != IntPtr.Zero ? objc_msgSend_ulong(resultsArray, sel_registerName("count")) : 0;

            var words = new List<OcrWordItem>();
            var lines = new List<OcrLineItem>();
            var fullTextSb = new StringBuilder();

            for (ulong i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();

                IntPtr observation = objc_msgSend_ulong_arg(resultsArray, sel_registerName("objectAtIndex:"), i);
                if (observation == IntPtr.Zero) continue;

                // [observation topCandidates:1] -> NSArray<VNRecognizedText>
                IntPtr candidates = objc_msgSend_ulong_arg(observation, sel_registerName("topCandidates:"), 1);
                ulong candCount = candidates != IntPtr.Zero ? objc_msgSend_ulong(candidates, sel_registerName("count")) : 0;
                if (candCount == 0) continue;

                IntPtr candidate = objc_msgSend_ulong_arg(candidates, sel_registerName("objectAtIndex:"), 0);
                if (candidate == IntPtr.Zero) continue;

                // String value
                IntPtr nsString = objc_msgSend(candidate, sel_registerName("string"));
                string text = GetUtf8String(nsString);
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Confidence
                float confidence = objc_msgSend_float(candidate, sel_registerName("confidence"));

                // BoundingBox: normalized CGRect in Vision coordinates (0,0 is bottom-left)
                NativeCGRect visionBox = GetObservationBoundingBox(observation);

                // Convert Vision coordinates (bottom-left) to top-left normalized coordinates
                // Vision: X=0..1 (left-to-right), Y=0..1 (bottom-to-top)
                // Top-left: X=X, Y=1.0 - (Y + Height)
                double topY = Math.Max(0.0, Math.Min(1.0, 1.0 - (visionBox.Y + visionBox.Height)));
                var lineBounds = new OcrBoundingBox(
                    Math.Max(0.0, visionBox.X),
                    topY,
                    Math.Max(0.001, visionBox.Width),
                    Math.Max(0.001, visionBox.Height));

                var lineItem = new OcrLineItem
                {
                    Text = text,
                    NormalizedBounds = lineBounds
                };

                // Split text into individual words and estimate/calculate sub-word bounding boxes
                string[] tokens = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0)
                {
                    double currentWordLeft = lineBounds.X;
                    double totalChars = Math.Max(1, text.Length);

                    foreach (var token in tokens)
                    {
                        double wordWidthFraction = (double)token.Length / totalChars;
                        double wordWidth = lineBounds.Width * wordWidthFraction;

                        var wordItem = new OcrWordItem
                        {
                            Text = token,
                            NormalizedBounds = new OcrBoundingBox(
                                Math.Max(0.0, currentWordLeft),
                                lineBounds.Y,
                                Math.Max(0.001, wordWidth),
                                lineBounds.Height),
                            Confidence = confidence
                        };

                        lineItem.Words.Add(wordItem);
                        words.Add(wordItem);

                        // Advance horizontal position including inter-word space
                        double spaceWidth = (lineBounds.Width / totalChars);
                        currentWordLeft += wordWidth + spaceWidth;
                    }
                }

                lines.Add(lineItem);
                fullTextSb.AppendLine(text);
            }

            return new OcrResult
            {
                Success = true,
                FullText = fullTextSb.ToString().TrimEnd(),
                Lines = lines,
                Words = words
            };
        }
        finally
        {
            if (autoreleasePool != IntPtr.Zero)
            {
                objc_msgSend(autoreleasePool, sel_registerName("drain"));
            }
        }
    }

    private static string GetUtf8String(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return string.Empty;
        IntPtr utf8Ptr = objc_msgSend(nsString, sel_registerName("UTF8String"));
        return utf8Ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(utf8Ptr) ?? string.Empty : string.Empty;
    }

    private static NativeCGRect GetObservationBoundingBox(IntPtr observation)
    {
        // On macOS 64-bit (both arm64 and x86_64), structs with 4 doubles can be retrieved
        // reliably through objc_msgSend with NativeCGRect return type.
        try
        {
            return objc_msgSend_stret_rect(observation, sel_registerName("boundingBox"));
        }
        catch
        {
            return new NativeCGRect { X = 0, Y = 0, Width = 1, Height = 0.05 };
        }
    }

    #region Objective-C Runtime P/Invoke

    [DllImport("libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr_ulong(IntPtr receiver, IntPtr selector, IntPtr arg1, ulong arg2);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_long(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_bool(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern ulong objc_msgSend_ulong(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_ulong_arg(IntPtr receiver, IntPtr selector, ulong index);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern float objc_msgSend_float(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_IntPtr_outIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, out IntPtr arg2);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern NativeCGRect objc_msgSend_stret_rect(IntPtr receiver, IntPtr selector);

    #endregion
}
