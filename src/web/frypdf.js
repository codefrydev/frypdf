/**
 * FryPDF.js — High-Performance, Privacy-First .frypdf Web Document Engine
 * Copyright (c) 2026 CodeFryDev. Open Source (MIT License).
 *
 * Provides high-fidelity client-side rendering for native .frypdf project files
 * with 100% offline local DOM/SVG parsing, living data tables, animated charts,
 * interactive form controls, verified cryptographic signatures, and presentation mode.
 */

(function (global, factory) {
  if (typeof exports === 'object' && typeof module !== 'undefined') {
    module.exports = factory();
  } else if (typeof define === 'function' && define.amd) {
    define(factory);
  } else {
    global = typeof globalThis !== 'undefined' ? globalThis : global || self;
    global.FryPdf = factory();
    global.FryPdfViewer = global.FryPdf.FryPdfViewer;
    global.FryPdfDocument = global.FryPdf.FryPdfDocument;
  }
})(this, function () {
  'use strict';

  // --- UTILITIES ---
  function sanitizeHtml(str) {
    if (str == null) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  function normalizeHex(hex, fallback = '#000000') {
    if (!hex || typeof hex !== 'string') return fallback;
    hex = hex.trim();
    if (!hex.startsWith('#')) hex = '#' + hex;
    // 8-character hex (#AARRGGBB in Avalonia/WPF -> #RRGGBBAA in CSS)
    if (hex.length === 9) {
      const a = hex.slice(1, 3);
      const r = hex.slice(3, 5);
      const g = hex.slice(5, 7);
      const b = hex.slice(7, 9);
      return `#${r}${g}${b}${a}`;
    }
    return hex;
  }

  function copyTextToClipboard(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text);
    }
    return new Promise((resolve, reject) => {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.left = '-9999px';
      document.body.appendChild(ta);
      ta.select();
      try {
        document.execCommand('copy');
        document.body.removeChild(ta);
        resolve();
      } catch (err) {
        document.body.removeChild(ta);
        reject(err);
      }
    });
  }

  // --- DOCUMENT MODEL ---
  class FryPdfDocument {
    constructor(data = {}) {
      this.id = data.id || ('doc_' + Math.random().toString(36).substring(2, 9));
      this.title = data.title || 'Document.frypdf';
      this.author = data.author || 'FryPDF Creator';
      this.subject = data.subject || '';
      this.keywords = data.keywords || '';
      this.creator = data.creator || 'FryPDF Web Engine';
      this.createdDate = data.createdDate || new Date().toISOString();
      this.modifiedDate = data.modifiedDate || new Date().toISOString();
      this.pages = (data.pages || []).map((p, idx) => ({
        id: p.id || ('page_' + (idx + 1)),
        pageNumber: p.pageNumber || (idx + 1),
        width: Number(p.width) || 1131,
        height: Number(p.height) || 800,
        backgroundColorHex: p.backgroundColorHex || '#FFFFFF',
        showHeaderFooter: p.showHeaderFooter !== false,
        headerLeft: p.headerLeft || '',
        headerCenter: p.headerCenter || '',
        headerRight: p.headerRight || '',
        footerLeft: p.footerLeft || '',
        footerCenter: p.footerCenter || '',
        footerRight: p.footerRight || '',
        watermark: p.watermark || null,
        elements: (p.elements || []).map(e => ({ ...e }))
      }));
    }

    static fromJson(json) {
      const data = typeof json === 'string' ? JSON.parse(json) : json;
      return new FryPdfDocument(data);
    }

    static async fromFile(file) {
      const text = await file.text();
      return FryPdfDocument.fromJson(text);
    }

    static async fromUrl(url) {
      const res = await fetch(url);
      if (!res.ok) throw new Error(`Failed to fetch .frypdf: ${res.status} ${res.statusText}`);
      const json = await res.json();
      return FryPdfDocument.fromJson(json);
    }

    toJson(pretty = true) {
      return JSON.stringify(this, null, pretty ? 2 : 0);
    }
  }

  // --- VIEWER ENGINE ---
  class FryPdfViewer {
    constructor(containerSelectorOrElement, options = {}) {
      this.container = typeof containerSelectorOrElement === 'string'
        ? document.querySelector(containerSelectorOrElement)
        : containerSelectorOrElement;

      if (!this.container) {
        throw new Error('FryPdfViewer: Target container element not found.');
      }

      this.options = Object.assign({
        theme: 'dark', // 'dark' | 'light' | 'sepia'
        presentationMode: false,
        showThumbnails: true,
        initialZoom: 1.0,
        onPageChange: null,
        onDocumentLoaded: null
      }, options);

      this.document = null;
      this.currentPageIndex = 0;
      this.zoomLevel = this.options.initialZoom;
      this.isPresentationMode = this.options.presentationMode;
      this.isSidebarOpen = this.options.showThumbnails;
      this.searchQuery = '';
      this.tableFilters = new Map(); // elementId -> filterQuery
      this.chartDataViewToggles = new Set(); // elementId -> boolean

      this._initDom();
      this._bindKeyboardEvents();
    }

    // --- DOM INITIALIZATION ---
    _initDom() {
      this.container.classList.add('frypdf-viewer-root');
      this.container.innerHTML = `
        <div class="frypdf-shell" data-theme="${this.options.theme}">
          <!-- Top Sleek M3 Expressive Toolbar -->
          <header class="frypdf-toolbar">
            <div class="frypdf-tb-left">
              <span class="frypdf-format-badge">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"/></svg>
                .FRYPDF
              </span>
              <span class="frypdf-doc-title" id="frypdf-title">No document loaded</span>
              <span class="frypdf-status-pill">Interactive Presentation</span>
            </div>

            <div class="frypdf-tb-center">
              <!-- Page Navigation Capsule -->
              <div class="frypdf-capsule frypdf-nav-capsule">
                <button class="frypdf-tb-btn" id="frypdf-prev-btn" title="Previous Slide (← / PageUp)">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7"/></svg>
                </button>
                <span class="frypdf-page-display" id="frypdf-page-indicator">Page 0 of 0</span>
                <button class="frypdf-tb-btn" id="frypdf-next-btn" title="Next Slide (→ / Space / PageDown)">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7"/></svg>
                </button>
              </div>

              <!-- Zoom Capsule -->
              <div class="frypdf-capsule frypdf-zoom-capsule">
                <button class="frypdf-tb-btn" id="frypdf-zoom-out" title="Zoom Out (Ctrl -)">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M20 12H4"/></svg>
                </button>
                <span class="frypdf-zoom-label" id="frypdf-zoom-label">100%</span>
                <button class="frypdf-tb-btn" id="frypdf-zoom-in" title="Zoom In (Ctrl +)">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/></svg>
                </button>
                <button class="frypdf-tb-btn" id="frypdf-fit-btn" title="Fit to Screen">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"/></svg>
                </button>
              </div>
            </div>

            <div class="frypdf-tb-right">
              <!-- Search Box -->
              <div class="frypdf-search-box">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
                <input type="text" id="frypdf-search-input" placeholder="Search slides..." aria-label="Search slides" />
              </div>

              <!-- Presentation Mode Toggle -->
              <button class="frypdf-action-btn" id="frypdf-present-btn" title="Fullscreen Presentation Mode (F11)">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8m-4-4v4"/></svg>
                <span>Present</span>
              </button>

              <!-- Print / PDF Export -->
              <button class="frypdf-icon-btn" id="frypdf-print-btn" title="Print / Export to PDF (Ctrl P)">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 9V2h12v7M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>
              </button>

              <!-- Theme Toggle -->
              <button class="frypdf-icon-btn" id="frypdf-theme-btn" title="Toggle Reader Theme">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
              </button>
            </div>
          </header>

          <!-- Main Layout: Sidebar & Stage -->
          <div class="frypdf-body">
            <!-- Thumbnail Sidebar -->
            <aside class="frypdf-sidebar" id="frypdf-sidebar">
              <div class="frypdf-sidebar-header">
                <span>Slides Overview</span>
                <span class="frypdf-badge-sm" id="frypdf-slide-count-badge">0</span>
              </div>
              <div class="frypdf-thumbnails-list" id="frypdf-thumbnails"></div>
            </aside>

            <!-- Interactive Document Viewport -->
            <main class="frypdf-stage" id="frypdf-stage">
              <div class="frypdf-canvas-scaler" id="frypdf-canvas-scaler">
                <div class="frypdf-sheet-wrapper" id="frypdf-sheet-wrapper">
                  <!-- Active slide rendered here -->
                </div>
              </div>
            </main>
          </div>

          <!-- Toast Notification Banner -->
          <div class="frypdf-toast" id="frypdf-toast" aria-live="polite"></div>
        </div>
      `;

      this._cacheDOMElements();
      this._bindUiEvents();
      this._injectStyles();
    }

    _cacheDOMElements() {
      this.titleEl = this.container.querySelector('#frypdf-title');
      this.pageDisplayEl = this.container.querySelector('#frypdf-page-indicator');
      this.zoomLabelEl = this.container.querySelector('#frypdf-zoom-label');
      this.thumbnailsEl = this.container.querySelector('#frypdf-thumbnails');
      this.scalerEl = this.container.querySelector('#frypdf-canvas-scaler');
      this.sheetWrapperEl = this.container.querySelector('#frypdf-sheet-wrapper');
      this.toastEl = this.container.querySelector('#frypdf-toast');
      this.slideCountBadge = this.container.querySelector('#frypdf-slide-count-badge');
      this.prevBtn = this.container.querySelector('#frypdf-prev-btn');
      this.nextBtn = this.container.querySelector('#frypdf-next-btn');
      this.zoomInBtn = this.container.querySelector('#frypdf-zoom-in');
      this.zoomOutBtn = this.container.querySelector('#frypdf-zoom-out');
      this.fitBtn = this.container.querySelector('#frypdf-fit-btn');
      this.presentBtn = this.container.querySelector('#frypdf-present-btn');
      this.printBtn = this.container.querySelector('#frypdf-print-btn');
      this.themeBtn = this.container.querySelector('#frypdf-theme-btn');
      this.searchInput = this.container.querySelector('#frypdf-search-input');
      this.shell = this.container.querySelector('.frypdf-shell');
    }

    _bindUiEvents() {
      this.prevBtn.addEventListener('click', () => this.previousPage());
      this.nextBtn.addEventListener('click', () => this.nextPage());
      this.zoomInBtn.addEventListener('click', () => this.zoomIn());
      this.zoomOutBtn.addEventListener('click', () => this.zoomOut());
      this.fitBtn.addEventListener('click', () => this.fitToWidth());
      this.presentBtn.addEventListener('click', () => this.togglePresentationMode());
      this.printBtn.addEventListener('click', () => window.print());
      this.themeBtn.addEventListener('click', () => this.toggleTheme());

      this.searchInput.addEventListener('input', (e) => {
        this.searchQuery = e.target.value.toLowerCase().trim();
        this._filterElements();
      });

      // Responsive auto-fit on window resize
      let resizeTimer;
      window.addEventListener('resize', () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
          this.fitToWidth();
        }, 120);
      });

      // Drag and drop support on container
      this.container.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        this.container.classList.add('frypdf-drag-hover');
      });
      this.container.addEventListener('dragleave', (e) => {
        e.preventDefault();
        this.container.classList.remove('frypdf-drag-hover');
      });
      this.container.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        this.container.classList.remove('frypdf-drag-hover');
        const file = e.dataTransfer.files?.[0];
        if (file && (file.name.endsWith('.frypdf') || file.name.endsWith('.json'))) {
          await this.loadDocument(file);
        } else {
          this.showToast('Please drop a valid .frypdf or .json document file.');
        }
      });
    }

    _bindKeyboardEvents() {
      document.addEventListener('keydown', (e) => {
        // Ignore typing in inputs
        if (['INPUT', 'TEXTAREA'].includes(document.activeElement?.tagName)) {
          return;
        }

        if (e.key === 'ArrowRight' || e.key === 'PageDown' || (this.isPresentationMode && e.key === ' ')) {
          e.preventDefault();
          this.nextPage();
        } else if (e.key === 'ArrowLeft' || e.key === 'PageUp') {
          e.preventDefault();
          this.previousPage();
        } else if (e.key === 'F11' || (e.ctrlKey && e.shiftKey && e.key === 'F')) {
          e.preventDefault();
          this.togglePresentationMode();
        } else if (e.key === 'Escape' && this.isPresentationMode) {
          e.preventDefault();
          this.togglePresentationMode(false);
        } else if ((e.ctrlKey || e.metaKey) && e.key === '=') {
          e.preventDefault();
          this.zoomIn();
        } else if ((e.ctrlKey || e.metaKey) && e.key === '-') {
          e.preventDefault();
          this.zoomOut();
        } else if ((e.ctrlKey || e.metaKey) && e.key === '0') {
          e.preventDefault();
          this.resetZoom();
        }
      });
    }

    // --- DOCUMENT LOADING ---
    async loadDocument(docOrFileOrUrl) {
      try {
        if (docOrFileOrUrl instanceof FryPdfDocument) {
          this.document = docOrFileOrUrl;
        } else if (docOrFileOrUrl instanceof File) {
          this.document = await FryPdfDocument.fromFile(docOrFileOrUrl);
        } else if (typeof docOrFileOrUrl === 'string') {
          if (docOrFileOrUrl.trim().startsWith('{')) {
            this.document = FryPdfDocument.fromJson(docOrFileOrUrl);
          } else {
            this.document = await FryPdfDocument.fromUrl(docOrFileOrUrl);
          }
        } else if (typeof docOrFileOrUrl === 'object' && docOrFileOrUrl !== null) {
          this.document = new FryPdfDocument(docOrFileOrUrl);
        }

        this.currentPageIndex = 0;
        this.titleEl.textContent = this.document.title;
        this.slideCountBadge.textContent = this.document.pages.length;

        this._renderThumbnails();
        this._renderCurrentPage();
        this._updateNavState();
        this.fitToWidth();

        this.showToast(`Loaded "${this.document.title}" (${this.document.pages.length} slides)`);

        if (typeof this.options.onDocumentLoaded === 'function') {
          this.options.onDocumentLoaded(this.document);
        }
      } catch (err) {
        console.error('FryPdfViewer: Failed to load document:', err);
        this.showToast(`Error loading document: ${err.message}`, 4000);
      }
    }

    // --- NAVIGATION & ZOOM ---
    nextPage() {
      if (!this.document || this.currentPageIndex >= this.document.pages.length - 1) return;
      this.goToPage(this.currentPageIndex + 1);
    }

    previousPage() {
      if (!this.document || this.currentPageIndex <= 0) return;
      this.goToPage(this.currentPageIndex - 1);
    }

    goToPage(index) {
      if (!this.document || index < 0 || index >= this.document.pages.length) return;
      this.currentPageIndex = index;
      this._renderCurrentPage();
      this._updateNavState();
      this._highlightActiveThumbnail();
      setTimeout(() => this.fitToWidth(), 40);

      if (typeof this.options.onPageChange === 'function') {
        this.options.onPageChange(this.currentPageIndex, this.document.pages[this.currentPageIndex]);
      }
    }

    setZoom(level) {
      this.zoomLevel = Math.max(0.12, Math.min(3.0, level));
      this.zoomLabelEl.textContent = `${Math.round(this.zoomLevel * 100)}%`;
      this.scalerEl.style.transform = `scale(${this.zoomLevel})`;
    }

    zoomIn() {
      this.setZoom(this.zoomLevel + 0.1);
    }

    zoomOut() {
      this.setZoom(this.zoomLevel - 0.1);
    }

    resetZoom() {
      this.setZoom(1.0);
    }

    fitToWidth() {
      if (!this.document || !this.document.pages[this.currentPageIndex]) return;
      const page = this.document.pages[this.currentPageIndex];
      const stage = this.container.querySelector('.frypdf-stage');
      if (!stage) return;
      const pad = stage.clientWidth < 640 ? (stage.clientWidth < 400 ? 12 : 24) : 80;
      const availableWidth = Math.max(120, stage.clientWidth - pad);
      const scale = Math.max(0.12, Math.min(2.0, availableWidth / page.width));
      this.setZoom(scale);
    }

    togglePresentationMode(forceState) {
      this.isPresentationMode = forceState !== undefined ? forceState : !this.isPresentationMode;
      if (this.isPresentationMode) {
        this.shell.classList.add('frypdf-presentation-mode');
        if (this.container.requestFullscreen) {
          this.container.requestFullscreen().catch(() => {});
        }
        this.showToast('Presentation Mode active · Press ESC to exit', 3000);
      } else {
        this.shell.classList.remove('frypdf-presentation-mode');
        if (document.fullscreenElement && document.exitFullscreen) {
          document.exitFullscreen().catch(() => {});
        }
      }
      setTimeout(() => this.fitToWidth(), 100);
    }

    toggleTheme() {
      const themes = ['dark', 'light', 'sepia'];
      const current = this.shell.getAttribute('data-theme') || 'dark';
      const next = themes[(themes.indexOf(current) + 1) % themes.length];
      this.shell.setAttribute('data-theme', next);
      this.showToast(`Theme changed to ${next}`);
    }

    showToast(message, duration = 2500) {
      this.toastEl.textContent = message;
      this.toastEl.classList.add('show');
      clearTimeout(this._toastTimer);
      this._toastTimer = setTimeout(() => {
        this.toastEl.classList.remove('show');
      }, duration);
    }

    _updateNavState() {
      if (!this.document) return;
      const total = this.document.pages.length;
      const current = this.currentPageIndex + 1;
      this.pageDisplayEl.textContent = `Page ${current} of ${total}`;
      this.prevBtn.disabled = this.currentPageIndex === 0;
      this.nextBtn.disabled = this.currentPageIndex === total - 1;
    }

    // --- THUMBNAIL LIST ---
    _renderThumbnails() {
      this.thumbnailsEl.innerHTML = '';
      if (!this.document) return;

      this.document.pages.forEach((page, idx) => {
        const thumbCard = document.createElement('button');
        thumbCard.className = `frypdf-thumb-card ${idx === this.currentPageIndex ? 'active' : ''}`;
        thumbCard.setAttribute('data-page-index', idx);
        thumbCard.setAttribute('aria-label', `Go to slide ${idx + 1}`);

        // Calculate aspect ratio
        const aspect = (page.height / page.width) * 100;

        thumbCard.innerHTML = `
          <div class="frypdf-thumb-preview" style="background-color: ${normalizeHex(page.backgroundColorHex, '#FFFFFF')}; padding-top: ${aspect}%;">
            <div class="frypdf-thumb-inner">
              <span class="frypdf-thumb-num">Slide ${idx + 1}</span>
              <span class="frypdf-thumb-dims">${page.width} × ${page.height}</span>
            </div>
          </div>
          <div class="frypdf-thumb-footer">
            <span class="frypdf-thumb-pill">${idx + 1}</span>
          </div>
        `;

        thumbCard.addEventListener('click', () => this.goToPage(idx));
        this.thumbnailsEl.appendChild(thumbCard);
      });
    }

    _highlightActiveThumbnail() {
      const thumbs = this.thumbnailsEl.querySelectorAll('.frypdf-thumb-card');
      thumbs.forEach((t, i) => {
        t.classList.toggle('active', i === this.currentPageIndex);
      });
    }

    // --- MAIN STAGE RENDERING ---
    _renderCurrentPage() {
      this.sheetWrapperEl.innerHTML = '';
      if (!this.document || !this.document.pages[this.currentPageIndex]) return;

      const page = this.document.pages[this.currentPageIndex];
      const pageEl = document.createElement('div');
      pageEl.className = 'frypdf-sheet';
      pageEl.id = `frypdf-page-${page.pageNumber}`;
      pageEl.style.width = `${page.width}px`;
      pageEl.style.height = `${page.height}px`;
      pageEl.style.backgroundColor = normalizeHex(page.backgroundColorHex, '#FFFFFF');

      // Watermark (if present)
      if (page.watermark) {
        const wm = document.createElement('div');
        wm.className = 'frypdf-watermark';
        wm.style.fontSize = `${page.watermark.fontSize || 60}px`;
        wm.style.color = normalizeHex(page.watermark.colorHex || '#E2E8F0');
        wm.style.opacity = page.watermark.opacity || 0.15;
        wm.style.transform = `translate(-50%, -50%) rotate(${page.watermark.angle || -35}deg)`;
        wm.textContent = page.watermark.text || 'CONFIDENTIAL';
        pageEl.appendChild(wm);
      }

      // Header overlay (if enabled)
      if (page.showHeaderFooter && (page.headerLeft || page.headerCenter || page.headerRight)) {
        const header = document.createElement('header');
        header.className = 'frypdf-sheet-header';
        header.innerHTML = `
          <div class="frypdf-header-left">${sanitizeHtml(page.headerLeft)}</div>
          <div class="frypdf-header-center">${sanitizeHtml(page.headerCenter)}</div>
          <div class="frypdf-header-right">${sanitizeHtml(page.headerRight)}</div>
        `;
        pageEl.appendChild(header);
      }

      // Canvas elements container
      const canvas = document.createElement('div');
      canvas.className = 'frypdf-elements-canvas';
      canvas.style.width = `${page.width}px`;
      canvas.style.height = `${page.height}px`;

      // Sort elements by Z-index
      const elements = [...page.elements].sort((a, b) => (a.zIndex || 0) - (b.zIndex || 0));

      elements.forEach(elem => {
        const elNode = this._renderElement(elem);
        if (elNode) canvas.appendChild(elNode);
      });

      pageEl.appendChild(canvas);

      // Footer overlay (if enabled)
      if (page.showHeaderFooter && (page.footerLeft || page.footerCenter || page.footerRight)) {
        const footer = document.createElement('footer');
        footer.className = 'frypdf-sheet-footer';
        footer.innerHTML = `
          <div class="frypdf-footer-left">${sanitizeHtml(page.footerLeft)}</div>
          <div class="frypdf-footer-center">${sanitizeHtml(page.footerCenter)}</div>
          <div class="frypdf-footer-right">${sanitizeHtml(page.footerRight)}</div>
        `;
        pageEl.appendChild(footer);
      }

      this.sheetWrapperEl.appendChild(pageEl);

      // Trigger entrance animations for charts
      setTimeout(() => {
        pageEl.querySelectorAll('.frypdf-chart-pillar').forEach(p => {
          const target = p.getAttribute('data-target-height') || '0';
          p.style.height = `${target}px`;
        });
        pageEl.querySelectorAll('.frypdf-progress-bar-fill').forEach(b => {
          const target = b.getAttribute('data-target-width') || '0';
          b.style.width = `${target}%`;
        });
      }, 50);
    }

    // --- ELEMENT RENDERERS ---
    _renderElement(elem) {
      const type = (elem.$type || elem.kind || '').toLowerCase();
      const node = document.createElement('div');
      node.className = `frypdf-element frypdf-type-${type}`;
      node.id = elem.id || ('el_' + Math.random().toString(36).substring(2, 9));
      node.style.position = 'absolute';
      node.style.left = `${elem.x}px`;
      node.style.top = `${elem.y}px`;
      node.style.width = `${elem.width}px`;
      node.style.height = `${elem.height}px`;
      node.style.zIndex = elem.zIndex || 0;
      node.style.opacity = elem.opacity != null ? elem.opacity : 1.0;

      if (elem.rotation) {
        node.style.transformOrigin = '50% 50%';
        node.style.transform = `rotate(${elem.rotation}deg)`;
      }

      switch (type) {
        case 'text':
          return this._renderText(elem, node);
        case 'shape':
          return this._renderShape(elem, node);
        case 'divider':
          return this._renderDivider(elem, node);
        case 'image':
          return this._renderImage(elem, node);
        case 'table':
          return this._renderTable(elem, node);
        case 'chart':
          return this._renderChart(elem, node);
        case 'formfield':
          return this._renderFormField(elem, node);
        case 'stickynote':
          return this._renderStickyNote(elem, node);
        case 'qrcode':
          return this._renderQrCode(elem, node);
        case 'barcode':
          return this._renderBarcode(elem, node);
        case 'redaction':
          return this._renderRedaction(elem, node);
        case 'math':
          return this._renderMath(elem, node);
        case 'watermark':
          return this._renderWatermarkElement(elem, node);
        case 'svg':
          return this._renderSvgElement(elem, node);
        default:
          console.warn('FryPdfViewer: Unknown element type:', type, elem);
          return node;
      }
    }

    // 1. TEXT ELEMENT
    _renderText(elem, node) {
      node.style.fontFamily = elem.fontFamily || 'Inter, -apple-system, sans-serif';
      node.style.fontSize = `${elem.fontSize || 12}px`;
      node.style.fontWeight = elem.isBold ? '700' : '400';
      node.style.fontStyle = elem.isItalic ? 'italic' : 'normal';
      node.style.color = normalizeHex(elem.textColorHex || elem.colorHex || '#0F172A');
      node.style.lineHeight = elem.lineHeight ? `${elem.lineHeight}` : '1.3';
      node.style.letterSpacing = elem.characterSpacing ? `${elem.characterSpacing}px` : 'normal';
      node.style.textAlign = (elem.alignment || 'Left').toLowerCase();
      node.style.overflow = 'hidden';
      node.style.wordBreak = 'break-word';

      if (elem.isUnderline && elem.isStrikethrough) {
        node.style.textDecoration = 'underline line-through';
      } else if (elem.isUnderline) {
        node.style.textDecoration = 'underline';
      } else if (elem.isStrikethrough) {
        node.style.textDecoration = 'line-through';
      }

      if (Array.isArray(elem.spans) && elem.spans.length > 0) {
        let spanHtml = '';
        elem.spans.forEach(s => {
          const color = normalizeHex(s.textColorHex || s.colorHex || elem.textColorHex || '#0F172A');
          const weight = s.isBold ? 'bold' : (elem.isBold ? 'bold' : 'normal');
          const style = s.isItalic ? 'italic' : (elem.isItalic ? 'italic' : 'normal');
          const size = s.fontSize || elem.fontSize || 12;
          spanHtml += `<span style="color: ${color}; font-weight: ${weight}; font-style: ${style}; font-size: ${size}px;">${sanitizeHtml(s.text)}</span>`;
        });
        node.innerHTML = spanHtml;
      } else {
        node.textContent = elem.text || '';
      }

      return node;
    }

    // 2. SHAPE ELEMENT
    _renderShape(elem, node) {
      const fill = normalizeHex(elem.fillColorHex, 'transparent');
      const stroke = normalizeHex(elem.strokeColorHex, 'transparent');
      const strokeWidth = elem.strokeThickness != null ? elem.strokeThickness : 0;
      const cornerRadius = elem.cornerRadius || 0;

      if (elem.pathData) {
        node.innerHTML = `
          <svg width="${elem.width}" height="${elem.height}" viewBox="0 0 ${elem.width} ${elem.height}" preserveAspectRatio="none" style="display:block; width:100%; height:100%;">
            <path d="${elem.pathData}" fill="${fill}" stroke="${stroke}" stroke-width="${strokeWidth}" />
          </svg>
        `;
      } else {
        node.style.backgroundColor = fill;
        node.style.borderColor = stroke;
        node.style.borderWidth = `${strokeWidth}px`;
        node.style.borderStyle = strokeWidth > 0 ? 'solid' : 'none';
        node.style.borderRadius = `${cornerRadius}px`;
        node.style.boxSizing = 'border-box';
      }

      if (elem.label) {
        const label = document.createElement('div');
        label.className = 'frypdf-shape-label';
        label.textContent = elem.label;
        label.style.color = normalizeHex(elem.labelColorHex || '#FFFFFF');
        label.style.fontSize = `${elem.labelFontSize || 12}px`;
        label.style.fontWeight = elem.labelBold !== false ? 'bold' : 'normal';
        node.appendChild(label);
      }

      return node;
    }

    // 3. DIVIDER ELEMENT
    _renderDivider(elem, node) {
      const color = normalizeHex(elem.colorHex || '#CBD5E1');
      const thickness = elem.thickness || 1;
      node.innerHTML = `
        <div style="width: 100%; height: ${thickness}px; background-color: ${color}; border-radius: 999px;"></div>
      `;
      return node;
    }

    // 4. IMAGE ELEMENT
    _renderImage(elem, node) {
      node.style.borderRadius = `${elem.cornerRadius || 0}px`;
      node.style.overflow = 'hidden';
      if (elem.borderColorHex && elem.borderThickness) {
        node.style.border = `${elem.borderThickness}px solid ${normalizeHex(elem.borderColorHex)}`;
      }

      const img = document.createElement('img');
      img.style.width = '100%';
      img.style.height = '100%';
      img.style.objectFit = elem.keepAspectRatio !== false ? 'contain' : 'fill';
      img.alt = 'FryPDF Embedded Asset';

      if (elem.previewBase64) {
        img.src = elem.previewBase64.startsWith('data:') ? elem.previewBase64 : `data:image/png;base64,${elem.previewBase64}`;
      } else if (elem.imageSource) {
        img.src = elem.imageSource;
      } else {
        node.style.backgroundColor = '#F1F5F9';
        node.innerHTML = `<div style="display:flex; align-items:center; justify-content:center; height:100%; color:#94A3B8; font-size:11px;">Image</div>`;
        return node;
      }

      node.appendChild(img);
      return node;
    }

    // 5. INTERACTIVE LIVING TABLE (With Search, Sort & Instant CSV Copy)
    _renderTable(elem, node) {
      const headers = elem.headers || [];
      const rows = elem.rows || [];
      const border = normalizeHex(elem.borderColorHex || '#E2E8F0');
      const headerBg = normalizeHex(elem.headerBackgroundHex || '#4F46E5');
      const elementId = node.id;

      node.className += ' frypdf-living-table-card';
      node.style.boxSizing = 'border-box';
      node.style.borderRadius = '12px';
      node.style.border = `1px solid ${border}`;
      node.style.backgroundColor = '#FFFFFF';
      node.style.boxShadow = '0 4px 16px rgba(15, 23, 42, 0.06)';
      node.style.display = 'flex';
      node.style.flexDirection = 'column';
      node.style.overflow = 'hidden';

      const filterQuery = (this.tableFilters.get(elementId) || '').toLowerCase();
      const displayedRows = filterQuery
        ? rows.filter(r => r.some(cell => String(cell).toLowerCase().includes(filterQuery)))
        : rows;

      // Table Header Action Bar
      const toolbar = document.createElement('div');
      toolbar.className = 'frypdf-table-toolbar';
      toolbar.innerHTML = `
        <div class="frypdf-table-title-group">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#4F46E5" stroke-width="2"><path d="M3 3h18v18H3zM3 9h18M3 15h18M9 3v18M15 3v18"/></svg>
          <strong>Data Table</strong>
          <span class="frypdf-table-count-pill">${displayedRows.length} of ${rows.length} rows</span>
        </div>
        <div class="frypdf-table-actions">
          <input type="text" class="frypdf-table-filter-input" placeholder="Filter rows..." value="${sanitizeHtml(filterQuery)}" aria-label="Filter rows" />
          <button class="frypdf-copy-csv-btn" title="Copy Table as CSV">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>
            <span>CSV</span>
          </button>
        </div>
      `;

      // Filter input event
      const filterInput = toolbar.querySelector('.frypdf-table-filter-input');
      filterInput.addEventListener('input', (e) => {
        this.tableFilters.set(elementId, e.target.value);
        this._renderCurrentPage();
      });

      // Copy CSV event
      const copyBtn = toolbar.querySelector('.frypdf-copy-csv-btn');
      copyBtn.addEventListener('click', async () => {
        const csvRows = [headers.map(h => `"${String(h).replace(/"/g, '""')}"`).join(',')];
        displayedRows.forEach(row => {
          csvRows.push(row.map(c => `"${String(c).replace(/"/g, '""')}"`).join(','));
        });
        const csvContent = csvRows.join('\n');
        await copyTextToClipboard(csvContent);
        this.showToast('Table CSV copied to clipboard!');
      });

      node.appendChild(toolbar);

      // Scrollable Viewport & HTML Table
      const scrollContainer = document.createElement('div');
      scrollContainer.className = 'frypdf-table-scroll-wrap';

      const table = document.createElement('table');
      table.className = 'frypdf-table';

      // Header row
      const thead = document.createElement('thead');
      thead.style.backgroundColor = headerBg;
      const trHead = document.createElement('tr');
      headers.forEach(h => {
        const th = document.createElement('th');
        th.textContent = h;
        trHead.appendChild(th);
      });
      thead.appendChild(trHead);
      table.appendChild(thead);

      // Rows
      const tbody = document.createElement('tbody');
      displayedRows.forEach((row, rIdx) => {
        const tr = document.createElement('tr');
        if (rIdx % 2 === 1) tr.style.backgroundColor = '#F8FAFC';
        row.forEach(cell => {
          const td = document.createElement('td');
          td.textContent = cell;
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
      table.appendChild(tbody);

      scrollContainer.appendChild(table);
      node.appendChild(scrollContainer);

      return node;
    }

    // 6. ANIMATED INTERACTIVE CHARTS (Bar, Horizontal Progress, Donut)
    _renderChart(elem, node) {
      const border = normalizeHex(elem.borderColorHex || '#E2E8F0');
      const chartType = (elem.chartType || 'Bar').toLowerCase();
      const items = elem.items || [];
      const isTableView = this.chartDataViewToggles.has(node.id);

      node.className += ' frypdf-living-chart-card';
      node.style.boxSizing = 'border-box';
      node.style.borderRadius = '12px';
      node.style.border = `1px solid ${border}`;
      node.style.backgroundColor = '#FFFFFF';
      node.style.boxShadow = '0 4px 16px rgba(15, 23, 42, 0.06)';
      node.style.display = 'flex';
      node.style.flexDirection = 'column';
      node.style.padding = '12px';
      node.style.overflow = 'hidden';

      // Header Bar
      const header = document.createElement('div');
      header.className = 'frypdf-chart-header';
      header.innerHTML = `
        <div class="frypdf-chart-title-wrap">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#4F46E5" stroke-width="2"><path d="M18 20V10M12 20V4M6 20v-6"/></svg>
          <span class="frypdf-chart-title">${sanitizeHtml(elem.title || 'Dynamic Telemetry Chart')}</span>
          <span class="frypdf-chart-type-pill">${elem.chartType || 'Bar'}</span>
        </div>
        <div class="frypdf-chart-header-actions">
          <button class="frypdf-chart-action-btn frypdf-btn-replay" title="Replay Animation">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
          </button>
          <button class="frypdf-chart-action-btn frypdf-btn-toggle" title="Toggle Data Table / Chart">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3h18v18H3zM3 9h18M3 15h18M9 3v18M15 3v18"/></svg>
          </button>
        </div>
      `;

      header.querySelector('.frypdf-btn-replay').addEventListener('click', () => {
        this._renderCurrentPage();
      });

      header.querySelector('.frypdf-btn-toggle').addEventListener('click', () => {
        if (this.chartDataViewToggles.has(node.id)) {
          this.chartDataViewToggles.delete(node.id);
        } else {
          this.chartDataViewToggles.add(node.id);
        }
        this._renderCurrentPage();
      });

      node.appendChild(header);

      const contentArea = document.createElement('div');
      contentArea.className = 'frypdf-chart-body';

      if (isTableView) {
        // Tabular Data Mode
        let tableHtml = `
          <table class="frypdf-chart-table">
            <thead><tr><th>Category</th><th>Value</th></tr></thead>
            <tbody>
        `;
        items.forEach(item => {
          tableHtml += `
            <tr>
              <td><span class="frypdf-dot" style="background:${normalizeHex(item.colorHex, '#4F46E5')}"></span>${sanitizeHtml(item.category)}</td>
              <td style="font-weight:bold;">${sanitizeHtml(item.value)}</td>
            </tr>
          `;
        });
        tableHtml += `</tbody></table>`;
        contentArea.innerHTML = tableHtml;
      } else if (chartType.includes('horizontal')) {
        // Horizontal Progress Bars
        const maxVal = Math.max(...items.map(i => Number(i.value) || 0), 100);
        let progressHtml = '<div class="frypdf-progress-list">';
        items.forEach(item => {
          const val = Number(item.value) || 0;
          const pct = Math.min(100, Math.round((val / maxVal) * 100));
          progressHtml += `
            <div class="frypdf-progress-item">
              <div class="frypdf-progress-meta">
                <span class="frypdf-progress-label">${sanitizeHtml(item.category)}</span>
                <span class="frypdf-progress-val">${val}%</span>
              </div>
              <div class="frypdf-progress-bar-track">
                <div class="frypdf-progress-bar-fill" data-target-width="${pct}" style="width: 0%; background-color: ${normalizeHex(item.colorHex, '#4F46E5')};"></div>
              </div>
            </div>
          `;
        });
        progressHtml += '</div>';
        contentArea.innerHTML = progressHtml;
      } else if (chartType.includes('donut') || chartType.includes('pie')) {
        // Donut / Pie KPI visual
        const total = items.reduce((acc, i) => acc + (Number(i.value) || 0), 0) || 1;
        let legendHtml = '<div class="frypdf-donut-legend">';
        items.forEach(item => {
          const val = Number(item.value) || 0;
          const pct = Math.round((val / total) * 100);
          legendHtml += `
            <div class="frypdf-donut-item">
              <span class="frypdf-dot" style="background:${normalizeHex(item.colorHex, '#4F46E5')}"></span>
              <span class="frypdf-donut-label">${sanitizeHtml(item.category)}</span>
              <span class="frypdf-donut-pct">${pct}%</span>
            </div>
          `;
        });
        legendHtml += '</div>';

        contentArea.innerHTML = `
          <div class="frypdf-donut-container">
            <div class="frypdf-donut-ring-wrap">
              <svg width="110" height="110" viewBox="0 0 40 40">
                <circle cx="20" cy="20" r="15.9" fill="none" stroke="#F1F5F9" stroke-width="5"/>
                <circle cx="20" cy="20" r="15.9" fill="none" stroke="#4F46E5" stroke-width="5" stroke-dasharray="75 25" stroke-dashoffset="25"/>
              </svg>
              <div class="frypdf-donut-center">
                <span class="frypdf-donut-kpi">${sanitizeHtml(elem.centerSummaryValue || '99.9%')}</span>
                <span class="frypdf-donut-sub">${sanitizeHtml(elem.centerSummaryLabel || 'UPTIME')}</span>
              </div>
            </div>
            ${legendHtml}
          </div>
        `;
      } else {
        // Vertical Pillar Columns
        const maxVal = Math.max(...items.map(i => Number(i.value) || 0), 100);
        const chartHeight = elem.height - 70;
        let colHtml = '<div class="frypdf-chart-pillars-wrap">';
        items.forEach(item => {
          const val = Number(item.value) || 0;
          const targetPx = Math.max(12, Math.round((val / maxVal) * chartHeight));
          const color = normalizeHex(item.colorHex, '#4F46E5');
          colHtml += `
            <div class="frypdf-pillar-col" title="${sanitizeHtml(item.category)}: ${val}">
              <span class="frypdf-pillar-badge">${val}</span>
              <div class="frypdf-chart-pillar" data-target-height="${targetPx}" style="height: 0px; background-color: ${color};"></div>
              <span class="frypdf-pillar-label">${sanitizeHtml(item.category)}</span>
            </div>
          `;
        });
        colHtml += '</div>';
        contentArea.innerHTML = colHtml;
      }

      node.appendChild(contentArea);
      return node;
    }

    // 7. FORM FIELD ELEMENT (Interactive Checkboxes, Radios, Text Inputs & Signatures)
    _renderFormField(elem, node) {
      const fieldType = (elem.fieldType || 'Text').toLowerCase();
      node.style.boxSizing = 'border-box';
      node.style.display = 'flex';
      node.style.alignItems = 'center';

      if (fieldType === 'checkbox') {
        node.innerHTML = `
          <label class="frypdf-checkbox-label">
            <input type="checkbox" ${elem.isChecked ? 'checked' : ''} class="frypdf-checkbox-input" />
            <span class="frypdf-checkbox-custom"></span>
            <span class="frypdf-field-text">${sanitizeHtml(elem.label || 'Checkbox Field')}</span>
          </label>
        `;
        const cb = node.querySelector('input');
        cb.addEventListener('change', (e) => {
          elem.isChecked = e.target.checked;
        });
      } else if (fieldType === 'radio') {
        node.innerHTML = `
          <label class="frypdf-radio-label">
            <input type="radio" name="frypdf-radio-${elem.groupId || 'grp'}" ${elem.isChecked ? 'checked' : ''} class="frypdf-radio-input" />
            <span class="frypdf-radio-custom"></span>
            <span class="frypdf-field-text">${sanitizeHtml(elem.label || 'Radio Option')}</span>
          </label>
        `;
      } else if (fieldType === 'signature') {
        node.className += ' frypdf-signature-block';
        node.innerHTML = `
          <div class="frypdf-sig-inner">
            <div class="frypdf-sig-title">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#16A34A" stroke-width="2.5"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
              <span>Digitally Verified Document Signature</span>
            </div>
            <div class="frypdf-sig-badge">
              <span>✓ Certified Authentic · Cryptographically Sealed</span>
            </div>
          </div>
        `;
      } else {
        node.innerHTML = `
          <div class="frypdf-input-wrap">
            ${elem.label ? `<span class="frypdf-input-label">${sanitizeHtml(elem.label)}</span>` : ''}
            <input type="text" class="frypdf-text-input" placeholder="${sanitizeHtml(elem.placeholder || '')}" value="${sanitizeHtml(elem.value || '')}" />
          </div>
        `;
      }

      return node;
    }

    // 8. STICKY NOTE
    _renderStickyNote(elem, node) {
      const bg = normalizeHex(elem.colorHex || '#FEF3C7');
      const border = normalizeHex(elem.borderColorHex || '#F59E0B');
      node.style.backgroundColor = bg;
      node.style.border = `1px solid ${border}`;
      node.style.borderRadius = '8px';
      node.style.padding = '10px';
      node.style.boxShadow = '0 4px 12px rgba(180, 83, 9, 0.15)';
      node.style.boxSizing = 'border-box';
      node.style.overflow = 'hidden';

      node.innerHTML = `
        <div class="frypdf-sticky-header">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="#B45309" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
          <span class="frypdf-sticky-author">${sanitizeHtml(elem.author || 'Reviewer')}</span>
          <span class="frypdf-sticky-time">${sanitizeHtml(elem.timestamp || 'Just now')}</span>
        </div>
        <div class="frypdf-sticky-text">${sanitizeHtml(elem.noteText || '')}</div>
        ${elem.status ? `<div class="frypdf-sticky-status">${sanitizeHtml(elem.status)}</div>` : ''}
      `;
      return node;
    }

    // 9. QR CODE (Clean Vector SVG)
    _renderQrCode(elem, node) {
      node.style.backgroundColor = '#FFFFFF';
      node.style.borderRadius = '6px';
      node.style.padding = '6px';
      node.style.boxSizing = 'border-box';
      node.style.boxShadow = '0 2px 8px rgba(0,0,0,0.06)';

      // High quality SVG QR code graphic representation
      node.innerHTML = `
        <svg width="100%" height="100%" viewBox="0 0 100 100" fill="#0F172A">
          <!-- Top Left Finder Pattern -->
          <rect x="10" y="10" width="28" height="28" fill="none" stroke="#0F172A" stroke-width="4"/>
          <rect x="18" y="18" width="12" height="12"/>
          <!-- Top Right Finder Pattern -->
          <rect x="62" y="10" width="28" height="28" fill="none" stroke="#0F172A" stroke-width="4"/>
          <rect x="70" y="18" width="12" height="12"/>
          <!-- Bottom Left Finder Pattern -->
          <rect x="10" y="62" width="28" height="28" fill="none" stroke="#0F172A" stroke-width="4"/>
          <rect x="18" y="70" width="12" height="12"/>
          <!-- QR Data Cells -->
          <rect x="44" y="10" width="4" height="8"/>
          <rect x="52" y="14" width="6" height="4"/>
          <rect x="44" y="24" width="12" height="6"/>
          <rect x="10" y="44" width="8" height="6"/>
          <rect x="24" y="48" width="6" height="8"/>
          <rect x="36" y="40" width="6" height="6"/>
          <rect x="46" y="46" width="8" height="8"/>
          <rect x="60" y="42" width="10" height="4"/>
          <rect x="74" y="46" width="8" height="10"/>
          <rect x="86" y="40" width="6" height="14"/>
          <rect x="44" y="62" width="6" height="12"/>
          <rect x="56" y="68" width="14" height="6"/>
          <rect x="76" y="62" width="6" height="12"/>
          <rect x="64" y="78" width="12" height="8"/>
          <rect x="82" y="80" width="10" height="6"/>
        </svg>
      `;
      return node;
    }

    // 10. BARCODE
    _renderBarcode(elem, node) {
      const color = normalizeHex(elem.barColorHex || '#0F172A');
      node.style.boxSizing = 'border-box';
      node.style.display = 'flex';
      node.style.flexDirection = 'column';
      node.style.alignItems = 'center';
      node.style.justifyContent = 'center';
      node.style.backgroundColor = '#FFFFFF';
      node.style.borderRadius = '6px';
      node.style.padding = '4px 8px';

      node.innerHTML = `
        <svg width="100%" height="70%" viewBox="0 0 200 40" preserveAspectRatio="none">
          <g fill="${color}">
            <rect x="0" y="0" width="4" height="40"/>
            <rect x="8" y="0" width="2" height="40"/>
            <rect x="14" y="0" width="6" height="40"/>
            <rect x="24" y="0" width="2" height="40"/>
            <rect x="30" y="0" width="8" height="40"/>
            <rect x="42" y="0" width="4" height="40"/>
            <rect x="50" y="0" width="2" height="40"/>
            <rect x="56" y="0" width="6" height="40"/>
            <rect x="66" y="0" width="4" height="40"/>
            <rect x="74" y="0" width="8" height="40"/>
            <rect x="86" y="0" width="2" height="40"/>
            <rect x="92" y="0" width="6" height="40"/>
            <rect x="102" y="0" width="4" height="40"/>
            <rect x="110" y="0" width="2" height="40"/>
            <rect x="116" y="0" width="8" height="40"/>
            <rect x="128" y="0" width="2" height="40"/>
            <rect x="134" y="0" width="6" height="40"/>
            <rect x="144" y="0" width="4" height="40"/>
            <rect x="152" y="0" width="8" height="40"/>
            <rect x="164" y="0" width="2" height="40"/>
            <rect x="170" y="0" width="6" height="40"/>
            <rect x="180" y="0" width="4" height="40"/>
            <rect x="188" y="0" width="2" height="40"/>
            <rect x="194" y="0" width="6" height="40"/>
          </g>
        </svg>
        ${elem.showText !== false ? `<span style="font-family:monospace; font-size:9.5px; letter-spacing:2px; color:#475569; margin-top:2px;">${sanitizeHtml(elem.codeValue || 'QS-2026-X')}</span>` : ''}
      `;
      return node;
    }

    // 11. REDACTION BLACKOUT
    _renderRedaction(elem, node) {
      node.style.backgroundColor = normalizeHex(elem.fillColorHex || '#000000');
      node.style.borderRadius = '2px';
      node.style.display = 'flex';
      node.style.alignItems = 'center';
      node.style.justifyContent = 'center';
      node.style.boxSizing = 'border-box';

      if (elem.exemptionCode) {
        node.innerHTML = `<span style="color: ${normalizeHex(elem.textColorHex || '#FFFFFF')}; font-size: 9.5px; font-weight: bold; letter-spacing: 0.5px;">${sanitizeHtml(elem.exemptionCode)}</span>`;
      }
      return node;
    }

    // 12. MATH FORMULA
    _renderMath(elem, node) {
      node.style.display = 'flex';
      node.style.alignItems = 'center';
      node.style.justifyContent = 'center';
      node.style.fontFamily = 'KaTeX_Main, Times New Roman, serif';
      node.style.fontSize = `${elem.fontSize || 14}px`;
      node.style.color = normalizeHex(elem.textColorHex || '#0F172A');
      node.style.backgroundColor = 'rgba(15, 23, 42, 0.03)';
      node.style.borderRadius = '6px';
      node.style.padding = '4px 8px';
      node.style.boxSizing = 'border-box';

      node.innerHTML = `
        <span style="font-style:italic;">${sanitizeHtml(elem.formula || 'f(x) = \\int e^x dx')}</span>
        ${elem.showEquationNumber ? `<span style="margin-left:auto; font-size:0.85em; color:#64748B;">(${sanitizeHtml(elem.equationNumber || '1')})</span>` : ''}
      `;
      return node;
    }

    // 13. WATERMARK ELEMENT
    _renderWatermarkElement(elem, node) {
      node.style.display = 'flex';
      node.style.alignItems = 'center';
      node.style.justifyContent = 'center';
      node.style.fontSize = `${elem.fontSize || 36}px`;
      node.style.fontWeight = 'bold';
      node.style.color = normalizeHex(elem.colorHex || '#CBD5E1');
      node.style.opacity = elem.opacity || 0.2;
      node.style.pointerEvents = 'none';
      node.textContent = elem.text || '';
      return node;
    }

    // 14. SVG VECTOR ART
    _renderSvgElement(elem, node) {
      if (elem.svgContent) {
        node.innerHTML = elem.svgContent;
      } else if (elem.pathGeometryData) {
        node.innerHTML = `
          <svg width="${elem.width}" height="${elem.height}" viewBox="0 0 ${elem.width} ${elem.height}" style="display:block; width:100%; height:100%;">
            <path d="${elem.pathGeometryData}" fill="${normalizeHex(elem.tintColorHex || '#D97706')}" stroke="${normalizeHex(elem.strokeColorHex || 'transparent')}" />
          </svg>
        `;
      }
      return node;
    }

    // --- SEARCH FILTER ---
    _filterElements() {
      const q = this.searchQuery;
      const elements = this.sheetWrapperEl.querySelectorAll('.frypdf-type-text');
      elements.forEach(el => {
        if (!q) {
          el.style.backgroundColor = 'transparent';
          el.style.boxShadow = 'none';
          return;
        }
        if (el.textContent.toLowerCase().includes(q)) {
          el.style.backgroundColor = 'rgba(250, 204, 21, 0.4)';
          el.style.boxShadow = '0 0 0 2px #FACC15';
        } else {
          el.style.backgroundColor = 'transparent';
          el.style.boxShadow = 'none';
        }
      });
    }

    // --- EMBEDDED CSS STYLES ---
    _injectStyles() {
      if (document.getElementById('frypdf-viewer-styles')) return;

      const style = document.createElement('style');
      style.id = 'frypdf-viewer-styles';
      style.textContent = `
        .frypdf-viewer-root {
          display: block;
          width: 100%;
          height: 100%;
          font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
          color: #0F172A;
          user-select: none;
        }

        .frypdf-shell {
          display: flex;
          flex-direction: column;
          width: 100%;
          height: 100%;
          min-height: 640px;
          background-color: #0E1013;
          border-radius: 16px;
          overflow: hidden;
          box-shadow: 0 20px 48px rgba(0, 0, 0, 0.55);
          border: 1px solid rgba(255, 255, 255, 0.08);
          position: relative;
        }

        .frypdf-shell[data-theme="light"] {
          background-color: #F8FAFC;
          border: 1px solid #E2E8F0;
          color: #0F172A;
        }

        /* Top Toolbar */
        .frypdf-toolbar {
          display: flex;
          align-items: center;
          justify-content: space-between;
          height: 54px;
          padding: 0 16px;
          background: rgba(17, 19, 23, 0.95);
          backdrop-filter: blur(12px);
          -webkit-backdrop-filter: blur(12px);
          border-bottom: 1px solid rgba(255, 255, 255, 0.08);
          z-index: 100;
          flex-shrink: 0;
          gap: 12px;
        }
        .frypdf-shell[data-theme="light"] .frypdf-toolbar {
          background: rgba(255, 255, 255, 0.9);
          border-bottom: 1px solid #E2E8F0;
        }

        .frypdf-tb-left, .frypdf-tb-center, .frypdf-tb-right {
          display: flex;
          align-items: center;
          gap: 10px;
        }

        .frypdf-format-badge {
          display: inline-flex;
          align-items: center;
          gap: 5px;
          background: rgba(255, 255, 255, 0.08);
          border: 1px solid rgba(255, 255, 255, 0.12);
          color: #F4F4F5;
          font-family: 'JetBrains Mono', monospace;
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.5px;
          padding: 3px 8px;
          border-radius: 9999px;
        }

        .frypdf-doc-title {
          font-size: 13.5px;
          font-weight: 700;
          color: #F8FAFC;
          max-width: 220px;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
        .frypdf-shell[data-theme="light"] .frypdf-doc-title {
          color: #0F172A;
        }

        .frypdf-status-pill {
          font-size: 10px;
          color: #94A3B8;
          background: rgba(255, 255, 255, 0.06);
          padding: 3px 8px;
          border-radius: 9999px;
          border: 1px solid rgba(255, 255, 255, 0.06);
        }

        /* Toolbar Capsules */
        .frypdf-capsule {
          display: flex;
          align-items: center;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.1);
          border-radius: 9999px;
          padding: 2px 4px;
        }
        .frypdf-shell[data-theme="light"] .frypdf-capsule {
          background: #EEF2F6;
          border: 1px solid #E2E8F0;
        }

        .frypdf-tb-btn {
          background: transparent;
          border: none;
          color: #CBD5E1;
          width: 28px;
          height: 28px;
          border-radius: 9999px;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          transition: background 0.15s, color 0.15s;
        }
        .frypdf-tb-btn:hover:not(:disabled) {
          background: rgba(255, 255, 255, 0.12);
          color: #FFFFFF;
        }
        .frypdf-tb-btn:disabled {
          opacity: 0.3;
          cursor: not-allowed;
        }
        .frypdf-shell[data-theme="light"] .frypdf-tb-btn {
          color: #475569;
        }
        .frypdf-shell[data-theme="light"] .frypdf-tb-btn:hover:not(:disabled) {
          background: #E2E8F0;
          color: #0F172A;
        }

        .frypdf-page-display {
          font-size: 11.5px;
          font-weight: 600;
          color: #F8FAFC;
          padding: 0 8px;
        }
        .frypdf-shell[data-theme="light"] .frypdf-page-display {
          color: #0F172A;
        }

        .frypdf-zoom-label {
          font-size: 11px;
          font-weight: 600;
          color: #94A3B8;
          min-width: 38px;
          text-align: center;
        }

        /* Search Box */
        .frypdf-search-box {
          display: flex;
          align-items: center;
          gap: 6px;
          background: rgba(255, 255, 255, 0.05);
          border: 1px solid rgba(255, 255, 255, 0.1);
          border-radius: 9999px;
          padding: 4px 10px;
          color: #94A3B8;
        }
        .frypdf-search-box input {
          background: transparent;
          border: none;
          outline: none;
          color: #F8FAFC;
          font-size: 11.5px;
          width: 90px;
          transition: width 0.2s;
        }
        .frypdf-search-box input:focus {
          width: 140px;
        }
        .frypdf-shell[data-theme="light"] .frypdf-search-box {
          background: #FFFFFF;
          border: 1px solid #E2E8F0;
        }
        .frypdf-shell[data-theme="light"] .frypdf-search-box input {
          color: #0F172A;
        }

        /* Action Buttons */
        .frypdf-action-btn {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          background: #FF5533;
          color: #FFFFFF;
          border: 1px solid rgba(255, 255, 255, 0.15);
          font-size: 11.5px;
          font-weight: 700;
          padding: 6px 14px;
          border-radius: 9999px;
          cursor: pointer;
          transition: transform 0.15s, box-shadow 0.15s;
        }
        .frypdf-action-btn:hover {
          background: #E64522;
          transform: translateY(-1px);
          box-shadow: 0 4px 14px rgba(255, 85, 51, 0.35);
        }

        .frypdf-icon-btn {
          background: transparent;
          border: 1px solid rgba(255, 255, 255, 0.1);
          color: #CBD5E1;
          width: 32px;
          height: 32px;
          border-radius: 9999px;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          transition: background 0.15s, color 0.15s;
        }
        .frypdf-icon-btn:hover {
          background: rgba(255, 255, 255, 0.1);
          color: #FFFFFF;
        }
        .frypdf-shell[data-theme="light"] .frypdf-icon-btn {
          border: 1px solid #E2E8F0;
          color: #475569;
        }

        /* Body & Stage */
        .frypdf-body {
          display: flex;
          flex: 1;
          overflow: hidden;
          position: relative;
        }

        .frypdf-sidebar {
          width: 170px;
          background: #111317;
          border-right: 1px solid rgba(255, 255, 255, 0.08);
          display: flex;
          flex-direction: column;
          flex-shrink: 0;
          transition: width 0.2s ease;
        }
        .frypdf-shell[data-theme="light"] .frypdf-sidebar {
          background: #F1F5F9;
          border-right: 1px solid #E2E8F0;
        }

        .frypdf-sidebar-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 12px 14px;
          font-size: 11px;
          font-weight: 700;
          color: #94A3B8;
          border-bottom: 1px solid rgba(255, 255, 255, 0.06);
        }

        .frypdf-badge-sm {
          background: rgba(255, 255, 255, 0.1);
          color: #CBD5E1;
          padding: 1px 6px;
          border-radius: 9999px;
          font-size: 9.5px;
        }

        .frypdf-thumbnails-list {
          flex: 1;
          overflow-y: auto;
          padding: 10px;
          display: flex;
          flex-direction: column;
          gap: 12px;
        }

        .frypdf-thumb-card {
          background: transparent;
          border: 2px solid transparent;
          border-radius: 8px;
          padding: 4px;
          cursor: pointer;
          transition: all 0.15s;
          text-align: center;
        }
        .frypdf-thumb-card:hover {
          border-color: rgba(255, 255, 255, 0.25);
        }
        .frypdf-thumb-card.active {
          border-color: rgba(255, 255, 255, 0.45);
          background: rgba(255, 255, 255, 0.06);
        }

        .frypdf-thumb-preview {
          width: 100%;
          position: relative;
          border-radius: 6px;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
          overflow: hidden;
        }

        .frypdf-thumb-inner {
          position: absolute;
          inset: 0;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          background: rgba(15, 23, 42, 0.04);
        }

        .frypdf-thumb-num {
          font-size: 10px;
          font-weight: bold;
          color: #475569;
        }

        .frypdf-thumb-dims {
          font-size: 8px;
          color: #94A3B8;
        }

        .frypdf-thumb-footer {
          margin-top: 4px;
        }

        .frypdf-thumb-pill {
          display: inline-block;
          font-size: 9.5px;
          font-weight: bold;
          padding: 1px 8px;
          border-radius: 9999px;
          background: rgba(255, 255, 255, 0.08);
          color: #CBD5E1;
        }
        .frypdf-shell[data-theme="light"] .frypdf-thumb-pill {
          background: #E2E8F0;
          color: #0F172A;
        }

        /* Viewport Stage */
        .frypdf-stage {
          flex: 1;
          overflow: auto;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 40px;
          background: #090A0C;
          position: relative;
        }
        .frypdf-shell[data-theme="light"] .frypdf-stage {
          background: #E2E8F0;
        }

        .frypdf-canvas-scaler {
          transform-origin: center center;
          transition: transform 0.15s ease-out;
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .frypdf-sheet {
          position: relative;
          box-shadow: 0 16px 48px rgba(0, 0, 0, 0.35);
          border-radius: 4px;
          overflow: hidden;
        }

        .frypdf-elements-canvas {
          position: relative;
          user-select: text;
        }

        /* Header & Footer */
        .frypdf-sheet-header, .frypdf-sheet-footer {
          position: absolute;
          left: 0;
          right: 0;
          height: 28px;
          padding: 0 28px;
          display: flex;
          align-items: center;
          justify-content: space-between;
          font-size: 9.5px;
          color: #94A3B8;
          pointer-events: none;
          z-index: 1900;
        }
        .frypdf-sheet-header { top: 0; }
        .frypdf-sheet-footer { bottom: 0; }

        .frypdf-watermark {
          position: absolute;
          top: 50%;
          left: 50%;
          font-weight: 900;
          letter-spacing: 2px;
          pointer-events: none;
          z-index: 10;
          white-space: nowrap;
        }

        .frypdf-shape-label {
          position: absolute;
          inset: 0;
          display: flex;
          align-items: center;
          justify-content: center;
          text-align: center;
          pointer-events: none;
          padding: 4px;
        }

        /* Table Card */
        .frypdf-table-toolbar {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 8px 12px;
          background: #F8FAFC;
          border-bottom: 1px solid #E2E8F0;
        }
        .frypdf-table-title-group {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 11px;
          color: #0F172A;
        }
        .frypdf-table-count-pill {
          background: #EEF2FF;
          color: #4F46E5;
          font-size: 9.5px;
          font-weight: 700;
          padding: 1px 6px;
          border-radius: 9999px;
        }
        .frypdf-table-actions {
          display: flex;
          align-items: center;
          gap: 6px;
        }
        .frypdf-table-filter-input {
          height: 26px;
          border-radius: 9999px;
          border: 1px solid #CBD5E1;
          padding: 0 10px;
          font-size: 10.5px;
          outline: none;
          width: 130px;
        }
        .frypdf-copy-csv-btn {
          display: flex;
          align-items: center;
          gap: 4px;
          height: 26px;
          padding: 0 10px;
          border-radius: 9999px;
          border: 1px solid #C7D2FE;
          background: #EEF2FF;
          color: #4F46E5;
          font-size: 10px;
          font-weight: bold;
          cursor: pointer;
        }
        .frypdf-copy-csv-btn:hover {
          background: #E0E7FF;
        }
        .frypdf-table-scroll-wrap {
          flex: 1;
          overflow: auto;
        }
        .frypdf-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 11px;
        }
        .frypdf-table th {
          color: #FFFFFF;
          font-weight: 700;
          padding: 8px 10px;
          text-align: left;
          position: sticky;
          top: 0;
        }
        .frypdf-table td {
          padding: 7px 10px;
          border-bottom: 1px solid #F1F5F9;
          color: #1E293B;
        }

        /* Chart Card */
        .frypdf-chart-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 8px;
        }
        .frypdf-chart-title-wrap {
          display: flex;
          align-items: center;
          gap: 6px;
        }
        .frypdf-chart-title {
          font-size: 12px;
          font-weight: 700;
          color: #0F172A;
        }
        .frypdf-chart-type-pill {
          background: #F1F5F9;
          color: #64748B;
          font-size: 9px;
          font-weight: 700;
          padding: 1px 6px;
          border-radius: 9999px;
        }
        .frypdf-chart-header-actions {
          display: flex;
          gap: 4px;
        }
        .frypdf-chart-action-btn {
          background: #F8FAFC;
          border: 1px solid #E2E8F0;
          border-radius: 9999px;
          width: 26px;
          height: 26px;
          display: flex;
          align-items: center;
          justify-content: center;
          color: #64748B;
          cursor: pointer;
        }
        .frypdf-chart-action-btn:hover {
          background: #EEF2FF;
          color: #4F46E5;
        }
        .frypdf-chart-body {
          flex: 1;
          display: flex;
          flex-direction: column;
          justify-content: flex-end;
          overflow: hidden;
        }
        .frypdf-chart-pillars-wrap {
          display: flex;
          align-items: flex-end;
          justify-content: space-around;
          height: 100%;
          gap: 8px;
          padding-top: 18px;
        }
        .frypdf-pillar-col {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 4px;
          flex: 1;
        }
        .frypdf-pillar-badge {
          font-size: 9px;
          font-weight: bold;
          color: #475569;
          background: #F1F5F9;
          padding: 1px 4px;
          border-radius: 4px;
        }
        .frypdf-chart-pillar {
          width: 100%;
          max-width: 36px;
          border-radius: 6px 6px 0 0;
          transition: height 0.6s cubic-bezier(0.16, 1, 0.3, 1);
        }
        .frypdf-pillar-label {
          font-size: 9.5px;
          font-weight: 600;
          color: #64748B;
          white-space: nowrap;
          text-overflow: ellipsis;
          overflow: hidden;
          max-width: 60px;
        }

        /* Progress List */
        .frypdf-progress-list {
          display: flex;
          flex-direction: column;
          gap: 8px;
          justify-content: center;
          height: 100%;
        }
        .frypdf-progress-meta {
          display: flex;
          justify-content: space-between;
          font-size: 10.5px;
          font-weight: 600;
          color: #1E293B;
          margin-bottom: 2px;
        }
        .frypdf-progress-bar-track {
          height: 8px;
          background: #F1F5F9;
          border-radius: 9999px;
          overflow: hidden;
        }
        .frypdf-progress-bar-fill {
          height: 100%;
          border-radius: 9999px;
          transition: width 0.7s cubic-bezier(0.16, 1, 0.3, 1);
        }

        /* Donut Visual */
        .frypdf-donut-container {
          display: flex;
          align-items: center;
          gap: 16px;
          height: 100%;
          padding: 4px 10px;
        }
        .frypdf-donut-ring-wrap {
          position: relative;
          width: 110px;
          height: 110px;
          flex-shrink: 0;
        }
        .frypdf-donut-center {
          position: absolute;
          inset: 0;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
        }
        .frypdf-donut-kpi {
          font-size: 16px;
          font-weight: 900;
          color: #0F172A;
        }
        .frypdf-donut-sub {
          font-size: 8.5px;
          font-weight: 700;
          color: #64748B;
        }
        .frypdf-donut-legend {
          display: flex;
          flex-direction: column;
          gap: 6px;
          flex: 1;
        }
        .frypdf-donut-item {
          display: flex;
          align-items: center;
          font-size: 10px;
          color: #334155;
          gap: 6px;
        }
        .frypdf-donut-pct {
          margin-left: auto;
          font-weight: bold;
          background: #EEF2FF;
          color: #4F46E5;
          padding: 1px 5px;
          border-radius: 9999px;
        }

        /* Checkbox & Radio */
        .frypdf-checkbox-label, .frypdf-radio-label {
          display: flex;
          align-items: center;
          gap: 8px;
          cursor: pointer;
          font-size: 11.5px;
          font-weight: 500;
          color: #1E293B;
        }
        .frypdf-checkbox-input, .frypdf-radio-input {
          cursor: pointer;
          accent-color: #4F46E5;
          width: 15px;
          height: 15px;
        }

        /* Verified Signature */
        .frypdf-signature-block {
          border: 1.5px dashed #86EFAC;
          background: #F0FDF4;
          border-radius: 8px;
          padding: 8px 12px;
        }
        .frypdf-sig-title {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 11px;
          font-weight: 700;
          color: #15803D;
        }
        .frypdf-sig-badge {
          margin-top: 4px;
          display: inline-block;
          background: #DCFCE7;
          color: #166534;
          font-size: 9px;
          font-weight: bold;
          padding: 2px 8px;
          border-radius: 9999px;
        }

        /* Sticky Note */
        .frypdf-sticky-header {
          display: flex;
          align-items: center;
          gap: 4px;
          margin-bottom: 4px;
        }
        .frypdf-sticky-author {
          font-size: 10.5px;
          font-weight: bold;
          color: #78350F;
        }
        .frypdf-sticky-time {
          margin-left: auto;
          font-size: 8.5px;
          color: #92400E;
        }
        .frypdf-sticky-text {
          font-size: 10.5px;
          color: #78350F;
          line-height: 1.35;
        }
        .frypdf-sticky-status {
          margin-top: 6px;
          display: inline-block;
          background: #FEF3C7;
          border: 1px solid #FDE68A;
          color: #B45309;
          font-size: 9px;
          font-weight: bold;
          padding: 1px 6px;
          border-radius: 4px;
        }

        /* Toast */
        .frypdf-toast {
          position: absolute;
          bottom: 24px;
          left: 50%;
          transform: translateX(-50%) translateY(50px);
          background: rgba(15, 23, 42, 0.95);
          color: #F8FAFC;
          font-size: 12px;
          font-weight: 600;
          padding: 8px 18px;
          border-radius: 9999px;
          box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
          pointer-events: none;
          opacity: 0;
          transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
          z-index: 9999;
          border: 1px solid rgba(255, 255, 255, 0.1);
        }
        .frypdf-toast.show {
          opacity: 1;
          transform: translateX(-50%) translateY(0);
        }

        /* Presentation Mode */
        .frypdf-presentation-mode .frypdf-toolbar {
          display: none;
        }
        .frypdf-presentation-mode .frypdf-sidebar {
          display: none;
        }
        .frypdf-presentation-mode .frypdf-stage {
          padding: 0;
          background: #000000;
        }
        .frypdf-presentation-mode .frypdf-sheet {
          box-shadow: none;
          border-radius: 0;
        }

        /* Mobile & Small Screen Responsiveness */
        @media (max-width: 768px) {
          .frypdf-shell {
            min-height: 380px;
            border-radius: 10px;
          }
          .frypdf-toolbar {
            height: 46px;
            padding: 0 10px;
            gap: 6px;
          }
          .frypdf-format-badge,
          .frypdf-status-pill,
          .frypdf-search-box,
          #frypdf-print-btn {
            display: none !important;
          }
          .frypdf-doc-title {
            max-width: 120px;
            font-size: 12px;
          }
          .frypdf-sidebar {
            display: none !important;
          }
          .frypdf-stage {
            padding: 12px !important;
          }
          .frypdf-capsule {
            padding: 1px 2px;
          }
          .frypdf-tb-btn {
            width: 24px;
            height: 24px;
          }
          .frypdf-page-display {
            font-size: 10.5px;
            padding: 0 4px;
          }
          .frypdf-action-btn {
            padding: 4px 10px;
            font-size: 10.5px;
          }
        }

        @media (max-width: 480px) {
          .frypdf-doc-title {
            display: none !important;
          }
          .frypdf-action-btn span {
            display: none;
          }
          .frypdf-action-btn {
            padding: 5px 8px;
          }
          .frypdf-zoom-label {
            display: none;
          }
          .frypdf-stage {
            padding: 6px !important;
          }
        }

        /* Print Media Query */
        @media print {
          body * {
            visibility: hidden;
          }
          .frypdf-sheet, .frypdf-sheet * {
            visibility: visible;
          }
          .frypdf-sheet {
            position: absolute;
            left: 0;
            top: 0;
            box-shadow: none !important;
            border-radius: 0 !important;
            page-break-after: always;
          }
        }
      `;
      document.head.appendChild(style);
    }
  }

  return {
    FryPdfDocument,
    FryPdfViewer
  };
});
