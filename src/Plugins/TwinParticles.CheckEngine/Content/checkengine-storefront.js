(function (window, document) {
  'use strict';

  var GUEST_KEY = 'ce.garage.guestKey';
  var GUEST_PAYLOAD_KEY = 'ce.garage.guestPayload';
  var SCRIPT_MARK = 'data-ce-storefront-loaded';

  if (document.documentElement.getAttribute(SCRIPT_MARK) === '1') {
    return;
  }
  document.documentElement.setAttribute(SCRIPT_MARK, '1');

  /* ------------------------------------------------------------------
   * Localised copy (rendered by the view; English fallbacks only apply
   * when the resource bundle is absent).
   * ------------------------------------------------------------------ */
  var TEXT = {
    garageLabel: 'Garage',
    garageSelect: 'Select vehicle',
    garageEmpty: 'No vehicle selected',
    garageAdd: 'Add vehicle (VIN)…',
    garageVinPrompt: 'Enter a VIN to add a vehicle',
    garageAddFailed: 'Unable to add that vehicle.',
    searchEmptyTitle: 'No matching parts found',
    searchEmptyHint: 'Check the OEM number or VIN, or widen fitment to include unverified parts.',
    searchUnavailable: 'Search is unavailable right now.',
    searchResultsCount: 'results',
    searchMode: 'Mode',
    fitsLabel: 'Fits your vehicle',
    fitsHint: 'Verified against your active vehicle.',
    unfitLabel: 'Does not fit',
    unfitHint: 'This part is not compatible with your active vehicle.',
    unknownLabel: 'Fitment unknown',
    unknownHint: 'We could not verify this part against your vehicle.',
    detailLabel: 'More vehicle detail needed',
    detailHint: 'This part fits some versions of your vehicle. Add the missing details to confirm.',
    detailCta: 'Complete vehicle details',
    selectLabel: 'Select your vehicle',
    selectHint: 'Choose a vehicle to check whether this part fits.',
    fitmentCta: 'Add your vehicle'
  };

  var MODE_NAMES = {
    1: 'Auto', 2: 'VIN', 3: 'OEM', 4: 'Vehicle', 5: 'Category', 6: 'Keyword', 7: 'Natural language'
  };

  var ICONS = {
    fits: '<circle cx="12" cy="12" r="9"/><path d="M8.5 12.5l2.5 2.5 4.5-5" stroke-linecap="round" stroke-linejoin="round"/>',
    unfit: '<circle cx="12" cy="12" r="9"/><path d="M9 9l6 6M15 9l-6 6" stroke-linecap="round"/>',
    unknown: '<circle cx="12" cy="12" r="9"/><path d="M8.5 12h7" stroke-linecap="round"/>',
    detail: '<circle cx="12" cy="12" r="9"/><path d="M9.5 9.5a2.5 2.5 0 1 1 3 2.45V14" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 17h.01" stroke-linecap="round"/>',
    select: '<circle cx="12" cy="12" r="9"/><path d="M12 8v4" stroke-linecap="round"/><path d="M12 16h.01" stroke-linecap="round"/>'
  };

  function loadText() {
    var node = document.getElementById('ce-i18n');
    if (!node) {
      return;
    }
    try {
      var parsed = JSON.parse(node.textContent || '{}');
      Object.keys(parsed).forEach(function (key) {
        var value = parsed[key];
        // A missing resource renders as the raw key; ignore those so the fallback wins.
        if (value && String(value).indexOf('Plugins.TwinParticles') !== 0) {
          TEXT[key] = value;
        }
      });
    } catch (e) {
      /* keep fallbacks */
    }
  }

  /* ------------------------------------------------------------------ */

  function ensureGuestKey() {
    try {
      var existing = window.localStorage.getItem(GUEST_KEY);
      if (existing) {
        return existing;
      }
      var key = createUuid();
      window.localStorage.setItem(GUEST_KEY, key);
      return key;
    } catch (e) {
      return createUuid();
    }
  }

  function createUuid() {
    if (window.crypto && typeof window.crypto.randomUUID === 'function') {
      return window.crypto.randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      var r = (Math.random() * 16) | 0;
      var v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  function jsonFetch(url, options) {
    options = options || {};
    options.headers = Object.assign({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    }, options.headers || {});
    return window.fetch(url, options);
  }

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function announce(message) {
    var status = document.getElementById('ce-search-status');
    if (status) {
      status.textContent = message;
    }
  }

  /* ------------------------------------------------------------------
   * Search
   * ------------------------------------------------------------------ */

  function resultsPanel() {
    return document.getElementById('ce-search-results');
  }

  function openResults(html) {
    var panel = resultsPanel();
    if (!panel) {
      return;
    }
    panel.innerHTML = html;
    panel.hidden = false;
    var input = document.getElementById('ce-sticky-search-input');
    if (input) {
      input.setAttribute('aria-expanded', 'true');
    }
  }

  function closeResults() {
    var panel = resultsPanel();
    if (!panel) {
      return;
    }
    panel.hidden = true;
    panel.innerHTML = '';
    var input = document.getElementById('ce-sticky-search-input');
    if (input) {
      input.setAttribute('aria-expanded', 'false');
    }
  }

  // Reserve layout space while loading so the panel does not shift content.
  function showSkeleton() {
    openResults(
      '<div class="ce-skeleton" aria-hidden="true">' +
      '<div class="ce-skeleton__row"></div>' +
      '<div class="ce-skeleton__row"></div>' +
      '<div class="ce-skeleton__row"></div>' +
      '</div>'
    );
  }

  function renderSearchResults(payload) {
    var hits = (payload && (payload.hits || payload.Hits)) || [];
    var modeUsed = payload && (payload.modeUsed || payload.ModeUsed);
    var degraded = payload && (payload.isDegraded || payload.IsDegraded);

    if (!hits.length) {
      openResults(
        '<div class="ce-results__empty">' +
        '<span class="ce-results__empty-title">' + escapeHtml(TEXT.searchEmptyTitle) + '</span>' +
        '<p class="ce-results__hint">' + escapeHtml(TEXT.searchEmptyHint) + '</p>' +
        '</div>'
      );
      announce(TEXT.searchEmptyTitle);
      return;
    }

    var header = '<div class="ce-results__header">' +
      '<span>' + hits.length + ' ' + escapeHtml(TEXT.searchResultsCount) + '</span>' +
      '<span class="ce-results__mode">' + escapeHtml(TEXT.searchMode) + ': ' +
      escapeHtml(MODE_NAMES[modeUsed] || String(modeUsed || '')) +
      (degraded ? ' · degraded' : '') +
      '</span></div>';

    var list = '<ul class="ce-results__list">';
    for (var i = 0; i < hits.length; i++) {
      var hit = hits[i];
      var name = hit.name || hit.Name || '';
      var id = hit.productId || hit.ProductId;
      var brand = hit.brand || hit.Brand || '';
      var fits = hit.fitsActiveContext || hit.FitsActiveContext;
      // The search contract returns no slug, so route through the host catalog search,
      // which resolves for any storefront configuration.
      var href = '/search?q=' + encodeURIComponent(name);

      list += '<li class="ce-results__item">' +
        '<a class="ce-results__link" href="' + href + '">' +
        '<span class="ce-results__body">' +
        '<span class="ce-results__name">' + escapeHtml(name) + '</span>' +
        '<span class="ce-results__meta">' +
        (brand ? escapeHtml(brand) + ' · ' : '') + '#' + escapeHtml(String(id)) +
        '</span></span>' +
        (fits ? '<span class="ce-results__badge ce-results__badge--fits">' + escapeHtml(TEXT.fitsLabel) + '</span>' : '') +
        '</a></li>';
    }
    list += '</ul>';

    openResults(header + list);
    announce(hits.length + ' ' + TEXT.searchResultsCount);
  }

  function runSearch() {
    var input = document.getElementById('ce-sticky-search-input');
    var widen = document.getElementById('ce-widen-fitment');
    var rawText = input ? input.value.trim() : '';

    if (!rawText) {
      closeResults();
      return;
    }

    showSkeleton();

    jsonFetch('/check-engine/search/query', {
      method: 'POST',
      body: JSON.stringify({
        rawText: rawText,
        page: 1,
        pageSize: 24,
        widenFitment: !!(widen && widen.checked),
        locale: document.documentElement.lang || 'en'
      })
    }).then(function (response) {
      if (!response.ok) {
        openResults('<div class="ce-results__empty"><span class="ce-results__empty-title">' +
          escapeHtml(TEXT.searchUnavailable) + '</span></div>');
        return null;
      }
      return response.json();
    }).then(function (payload) {
      if (payload) {
        renderSearchResults(payload);
      }
    }).catch(function () {
      openResults('<div class="ce-results__empty"><span class="ce-results__empty-title">' +
        escapeHtml(TEXT.searchUnavailable) + '</span></div>');
    });
  }

  function bindSearch() {
    var form = document.getElementById('ce-search-form');
    if (!form) {
      return;
    }

    form.addEventListener('submit', function (event) {
      event.preventDefault();
      runSearch();
    });

    var widen = document.getElementById('ce-widen-fitment');
    if (widen) {
      widen.addEventListener('change', function () {
        var panel = resultsPanel();
        if (panel && !panel.hidden) {
          runSearch();
        }
      });
    }

    // Escape closes the panel and returns focus to the field.
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape') {
        var panel = resultsPanel();
        if (panel && !panel.hidden) {
          closeResults();
          var input = document.getElementById('ce-sticky-search-input');
          if (input) {
            input.focus();
          }
        }
      }
    });

    // Clicking away dismisses the dropdown.
    document.addEventListener('click', function (event) {
      var panel = resultsPanel();
      if (!panel || panel.hidden) {
        return;
      }
      var rail = document.querySelector('[data-ce-theme="sticky-search"]');
      if (rail && !rail.contains(event.target)) {
        closeResults();
      }
    });
  }

  /* ------------------------------------------------------------------
   * Garage context
   * ------------------------------------------------------------------ */

  function getGuestPayload() {
    try {
      var raw = window.localStorage.getItem(GUEST_PAYLOAD_KEY);
      if (!raw) {
        return { vehicles: [], oems: [], activeVehicleId: null };
      }
      return JSON.parse(raw);
    } catch (e) {
      return { vehicles: [], oems: [], activeVehicleId: null };
    }
  }

  function saveGuestPayload(payload) {
    try {
      window.localStorage.setItem(GUEST_PAYLOAD_KEY, JSON.stringify(payload || {}));
    } catch (e) {
      /* ignore storage failures */
    }
  }

  function loadGarageContext() {
    return jsonFetch('/check-engine/garage/Current', { method: 'GET' }).then(function (response) {
      if (!response.ok) {
        return { guest: true, garage: getGuestPayload() };
      }
      return response.json().then(function (garage) {
        return { guest: false, garage: garage };
      });
    }).catch(function () {
      return { guest: true, garage: getGuestPayload() };
    });
  }

  function activeVehicleOf(garage) {
    if (!garage) {
      return null;
    }
    var vehicles = garage.vehicles || garage.Vehicles || [];
    var activeId = garage.activeGarageVehicleId || garage.ActiveGarageVehicleId ||
      garage.activeVehicleId || garage.ActiveVehicleId;
    for (var i = 0; i < vehicles.length; i++) {
      var vehicle = vehicles[i];
      var id = vehicle.id || vehicle.Id;
      if ((activeId != null && String(id) === String(activeId)) || vehicle.isActive || vehicle.IsActive) {
        return vehicle;
      }
    }
    return null;
  }

  function updateGarageChip(ctx) {
    var chip = document.getElementById('ce-garage-chip');
    var value = document.getElementById('ce-garage-chip-value');
    if (!chip || !value) {
      return;
    }
    var active = activeVehicleOf(ctx.garage);
    if (active) {
      value.textContent = active.label || active.Label || TEXT.garageSelect;
      chip.setAttribute('data-ce-state', 'active');
    } else {
      value.textContent = TEXT.garageSelect;
      chip.setAttribute('data-ce-state', 'empty');
    }
  }

  function populateVehicleSelector() {
    var select = document.getElementById('ce-vehicle-selector');
    if (!select) {
      return Promise.resolve(null);
    }

    return loadGarageContext().then(function (ctx) {
      var vehicles = (ctx.garage && (ctx.garage.vehicles || ctx.garage.Vehicles)) || [];
      var active = activeVehicleOf(ctx.garage);

      select.innerHTML = '';
      var placeholder = document.createElement('option');
      placeholder.value = '';
      placeholder.textContent = vehicles.length ? TEXT.garageSelect : TEXT.garageEmpty;
      select.appendChild(placeholder);

      vehicles.forEach(function (vehicle) {
        var option = document.createElement('option');
        option.value = String(vehicle.id || vehicle.Id || '');
        option.textContent = vehicle.label || vehicle.Label || ('#' + option.value);
        if (active && String(active.id || active.Id) === option.value) {
          option.selected = true;
        }
        select.appendChild(option);
      });

      var addOption = document.createElement('option');
      addOption.value = '__add__';
      addOption.textContent = TEXT.garageAdd;
      select.appendChild(addOption);

      updateGarageChip(ctx);
      return ctx;
    });
  }

  function addVehicleByVin(vin) {
    return jsonFetch('/check-engine/garage/AddVehicle', {
      method: 'POST',
      body: JSON.stringify({ vin: vin })
    }).then(function (response) {
      if (response.status === 401) {
        var payload = getGuestPayload();
        payload.vehicles = payload.vehicles || [];
        var nextId = payload.vehicles.reduce(function (max, v) {
          return Math.max(max, v.id || 0);
        }, 0) + 1;
        payload.vehicles.push({
          id: nextId,
          vin: vin,
          label: 'VIN ' + vin,
          isActive: payload.vehicles.length === 0
        });
        if (!payload.activeVehicleId) {
          payload.activeVehicleId = nextId;
        }
        saveGuestPayload(payload);
        return;
      }
      if (!response.ok) {
        window.alert(TEXT.garageAddFailed);
      }
    });
  }

  function setActiveVehicle(garageVehicleId) {
    return jsonFetch('/check-engine/garage/SetActive', {
      method: 'PUT',
      body: JSON.stringify({ garageVehicleId: garageVehicleId })
    }).then(function (response) {
      if (response.status === 401) {
        var payload = getGuestPayload();
        payload.activeVehicleId = garageVehicleId;
        (payload.vehicles || []).forEach(function (v) {
          v.isActive = (v.id === garageVehicleId);
        });
        saveGuestPayload(payload);
      }
    });
  }

  function promptForVin() {
    var vin = window.prompt(TEXT.garageVinPrompt);
    if (!vin) {
      return Promise.resolve(false);
    }
    return addVehicleByVin(vin.trim()).then(function () {
      return populateVehicleSelector();
    }).then(function () {
      evaluateFitmentBand();
      return true;
    });
  }

  function bindGarage() {
    var select = document.getElementById('ce-vehicle-selector');
    if (select) {
      select.addEventListener('change', function () {
        var value = select.value;
        if (!value) {
          return;
        }
        if (value === '__add__') {
          select.value = '';
          promptForVin();
          return;
        }
        setActiveVehicle(parseInt(value, 10)).then(function () {
          return populateVehicleSelector();
        }).then(function () {
          evaluateFitmentBand();
        });
      });
    }

    var chip = document.getElementById('ce-garage-chip');
    if (chip) {
      chip.addEventListener('click', function () {
        if (select) {
          select.focus();
        }
      });
    }
  }

  /* ------------------------------------------------------------------
   * Fitment band
   * ------------------------------------------------------------------ */

  function readProductId(band) {
    if (band && band.getAttribute('data-ce-product-id')) {
      var attr = parseInt(band.getAttribute('data-ce-product-id'), 10);
      if (attr > 0) {
        return attr;
      }
    }
    var named = document.querySelector('input[name="product_id"]');
    if (named && named.value) {
      var fromInput = parseInt(named.value, 10);
      if (fromInput > 0) {
        return fromInput;
      }
    }
    // Fall back to the host theme's own markers so the band keeps working if the widget zone
    // ever renders without a product id.
    var marked = document.querySelector('#product-details-form [data-productid], .product-item[data-productid], [data-productid]');
    if (marked) {
      var fromMarked = parseInt(marked.getAttribute('data-productid'), 10);
      if (fromMarked > 0) {
        return fromMarked;
      }
    }
    var addToCart = document.querySelector('[id^="addtocart_"]');
    if (addToCart) {
      var fromAddToCart = parseInt(addToCart.id.replace('addtocart_', ''), 10);
      if (fromAddToCart > 0) {
        return fromAddToCart;
      }
    }
    return 0;
  }

  function setFitmentState(band, state) {
    var map = {
      fits: [TEXT.fitsLabel, TEXT.fitsHint],
      unfit: [TEXT.unfitLabel, TEXT.unfitHint],
      unknown: [TEXT.unknownLabel, TEXT.unknownHint],
      detail: [TEXT.detailLabel, TEXT.detailHint],
      select: [TEXT.selectLabel, TEXT.selectHint]
    };
    var copy = map[state] || map.unknown;

    band.setAttribute('data-ce-fit', state);
    band.innerHTML =
      '<svg class="ce-icon ce-fitment__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true" focusable="false">' +
      ICONS[state] + '</svg>' +
      '<span class="ce-fitment__text">' +
      '<span class="ce-fitment__status">' + escapeHtml(copy[0]) + '</span>' +
      '<span class="ce-fitment__detail">' + escapeHtml(copy[1]) + '</span>' +
      '</span>';

    if (state === 'select' || state === 'detail') {
      var action = document.createElement('button');
      action.type = 'button';
      action.className = 'ce-btn ce-btn--quiet ce-fitment__action';
      action.textContent = state === 'detail' ? TEXT.detailCta : TEXT.fitmentCta;
      action.addEventListener('click', function () {
        promptForVin();
      });
      band.appendChild(action);
    }
  }

  function evaluateFitmentBand() {
    var band = document.querySelector('[data-ce-theme="fitment-band"]');
    if (!band) {
      return;
    }
    var productId = readProductId(band);
    if (!productId) {
      setFitmentState(band, 'select');
      return;
    }

    loadGarageContext().then(function (ctx) {
      var active = activeVehicleOf(ctx.garage);
      var configurationId = active &&
        (active.vehicleConfigurationId || active.VehicleConfigurationId);

      if (!configurationId) {
        setFitmentState(band, 'select');
        return null;
      }

      return jsonFetch('/check-engine/fitment/evaluate', {
        method: 'POST',
        body: JSON.stringify({ productId: productId, vehicleConfigurationId: configurationId })
      }).then(function (response) {
        if (!response.ok) {
          setFitmentState(band, 'unknown');
          return;
        }
        return response.json().then(function (result) {
          var outcome = result.outcome != null ? result.outcome : result.Outcome;
          if (outcome === 1 || outcome === 'Fits') {
            setFitmentState(band, 'fits');
          } else if (outcome === 2 || outcome === 'DoesNotFit') {
            setFitmentState(band, 'unfit');
          } else if (outcome === 5 || outcome === 'NeedsDisambiguation') {
            // Not a fit: the claim depends on vehicle details we do not have yet.
            setFitmentState(band, 'detail');
          } else {
            // Fail closed: anything we cannot verify is reported as unknown, never as a fit.
            setFitmentState(band, 'unknown');
          }
        });
      });
    }).catch(function () {
      setFitmentState(band, 'unknown');
    });
  }

  /* ------------------------------------------------------------------
   * Hero calls to action
   * ------------------------------------------------------------------ */

  function bindHero() {
    document.querySelectorAll('[data-ce-action]').forEach(function (button) {
      button.addEventListener('click', function () {
        var action = button.getAttribute('data-ce-action');
        if (action === 'focus-search') {
          var input = document.getElementById('ce-sticky-search-input');
          if (input) {
            input.focus();
            input.scrollIntoView({ block: 'center', behavior: 'smooth' });
          }
        } else if (action === 'add-vehicle') {
          promptForVin();
        }
      });
    });
  }

  /* ------------------------------------------------------------------ */

  function maybeMigrateGuestOnLogin() {
    var path = (window.location.pathname || '').toLowerCase();
    if (path.indexOf('login') === -1 && path.indexOf('customer/info') === -1) {
      return;
    }
    var guestKey = ensureGuestKey();
    jsonFetch('/check-engine/garage/Migrate', {
      method: 'POST',
      body: JSON.stringify({ guestKey: guestKey })
    }).then(function (response) {
      if (response.ok) {
        try {
          window.localStorage.removeItem(GUEST_PAYLOAD_KEY);
        } catch (e) {
          /* ignore */
        }
      }
    }).catch(function () {
      /* migration is best-effort */
    });
  }

  function boot() {
    loadText();
    ensureGuestKey();
    bindSearch();
    bindGarage();
    bindHero();
    populateVehicleSelector();
    evaluateFitmentBand();
    maybeMigrateGuestOnLogin();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})(window, document);
