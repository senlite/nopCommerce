(function (window, document) {
  'use strict';

  var GUEST_KEY = 'ce.garage.guestKey';
  var GUEST_PAYLOAD_KEY = 'ce.garage.guestPayload';
  var SCRIPT_MARK = 'data-ce-storefront-loaded';

  if (document.documentElement.getAttribute(SCRIPT_MARK) === '1') {
    return;
  }
  document.documentElement.setAttribute(SCRIPT_MARK, '1');

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
    return window.fetch(url, options).then(function (response) {
      return response;
    });
  }

  function getOrCreateResultsPanel() {
    var panel = document.getElementById('ce-search-results');
    if (panel) {
      return panel;
    }
    var sticky = document.querySelector('[data-ce-theme="sticky-search"]');
    panel = document.createElement('div');
    panel.id = 'ce-search-results';
    panel.className = 'ce-search-results';
    panel.setAttribute('aria-live', 'polite');
    if (sticky) {
      sticky.appendChild(panel);
    } else {
      document.body.appendChild(panel);
    }
    return panel;
  }

  function renderSearchResults(payload) {
    var panel = getOrCreateResultsPanel();
    var hits = (payload && payload.hits) || (payload && payload.Hits) || [];
    if (!hits.length) {
      panel.innerHTML = '<p class="ce-search-empty">No matching parts found.</p>';
      return;
    }
    var html = '<ul class="ce-search-results-list">';
    for (var i = 0; i < hits.length; i++) {
      var hit = hits[i];
      var name = hit.name || hit.Name || ('Product ' + (hit.productId || hit.ProductId));
      var id = hit.productId || hit.ProductId;
      var score = hit.score != null ? hit.score : hit.Score;
      html += '<li class="ce-search-result-item"><a href="/product/' + encodeURIComponent(id) + '">' +
        escapeHtml(name) + '</a>' +
        (score != null ? ' <span class="ce-search-score">(' + score + ')</span>' : '') +
        '</li>';
    }
    html += '</ul>';
    panel.innerHTML = html;
  }

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function bindStickySearch() {
    var form = document.querySelector('[data-ce-theme="sticky-search"] form');
    if (!form) {
      return;
    }
    form.addEventListener('submit', function (event) {
      event.preventDefault();
      var input = form.querySelector('input[name="rawText"], #ce-sticky-search-input');
      var rawText = input ? input.value : '';
      jsonFetch('/check-engine/search/query', {
        method: 'POST',
        body: JSON.stringify({ rawText: rawText, page: 1, pageSize: 24, locale: 'en' })
      }).then(function (response) {
        if (!response.ok) {
          getOrCreateResultsPanel().innerHTML = '<p class="ce-search-empty">Search unavailable.</p>';
          return null;
        }
        return response.json();
      }).then(function (payload) {
        if (payload) {
          renderSearchResults(payload);
        }
      }).catch(function () {
        getOrCreateResultsPanel().innerHTML = '<p class="ce-search-empty">Search unavailable.</p>';
      });
    });
  }

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
    var form = document.getElementById('product-details-form');
    if (form) {
      var dataId = form.getAttribute('data-productid') || form.getAttribute('data-product-id');
      if (dataId) {
        var fromForm = parseInt(dataId, 10);
        if (fromForm > 0) {
          return fromForm;
        }
      }
    }
    var match = window.location.pathname.match(/\/(\d+)(?:\/|$)/);
    if (match) {
      return parseInt(match[1], 10);
    }
    return 0;
  }

  function setFitmentBandState(band, text, cssClass) {
    band.classList.remove('ce-fit-fits', 'ce-fit-nofit', 'ce-fit-unknown', 'ce-fit-select');
    if (cssClass) {
      band.classList.add(cssClass);
    }
    band.innerHTML = '<strong>Fitment:</strong> ' + escapeHtml(text);
  }

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
      // ignore storage failures
    }
  }

  function resolveActiveVehicleConfigurationId(garage) {
    if (!garage) {
      return null;
    }
    var vehicles = garage.vehicles || garage.Vehicles || [];
    var activeId = garage.activeGarageVehicleId || garage.ActiveGarageVehicleId || garage.activeVehicleId || garage.ActiveVehicleId;
    for (var i = 0; i < vehicles.length; i++) {
      var vehicle = vehicles[i];
      var id = vehicle.id || vehicle.Id;
      if (activeId != null && id === activeId) {
        return vehicle.vehicleConfigurationId || vehicle.VehicleConfigurationId || null;
      }
      if (vehicle.isActive || vehicle.IsActive) {
        return vehicle.vehicleConfigurationId || vehicle.VehicleConfigurationId || null;
      }
    }
    return null;
  }

  function loadGarageContext() {
    return jsonFetch('/check-engine/garage/Current', { method: 'GET' }).then(function (response) {
      if (response.status === 401) {
        return { guest: true, garage: getGuestPayload() };
      }
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

  function evaluateFitmentBand() {
    var band = document.querySelector('[data-ce-theme="fitment-band"]');
    if (!band) {
      return;
    }
    var productId = readProductId(band);
    if (!productId) {
      setFitmentBandState(band, 'Select your vehicle', 'ce-fit-select');
      return;
    }
    loadGarageContext().then(function (ctx) {
      var vehicleConfigurationId = resolveActiveVehicleConfigurationId(ctx.garage);
      if (!vehicleConfigurationId) {
        setFitmentBandState(band, 'Select your vehicle', 'ce-fit-select');
        return;
      }
      return jsonFetch('/check-engine/fitment/evaluate', {
        method: 'POST',
        body: JSON.stringify({
          productId: productId,
          vehicleConfigurationId: vehicleConfigurationId
        })
      }).then(function (response) {
        if (!response.ok) {
          setFitmentBandState(band, 'Unknown', 'ce-fit-unknown');
          return;
        }
        return response.json().then(function (result) {
          var outcome = result.outcome != null ? result.outcome : result.Outcome;
          if (outcome === 1 || outcome === 'Fits') {
            setFitmentBandState(band, 'Fits', 'ce-fit-fits');
          } else if (outcome === 2 || outcome === 'DoesNotFit') {
            setFitmentBandState(band, 'Does not fit', 'ce-fit-nofit');
          } else {
            setFitmentBandState(band, 'Unknown', 'ce-fit-unknown');
          }
        });
      });
    }).catch(function () {
      setFitmentBandState(band, 'Unknown', 'ce-fit-unknown');
    });
  }

  function populateVehicleSelector() {
    var select = document.getElementById('ce-vehicle-selector');
    if (!select) {
      return;
    }
    loadGarageContext().then(function (ctx) {
      var vehicles = (ctx.garage && (ctx.garage.vehicles || ctx.garage.Vehicles)) || [];
      var activeId = ctx.garage && (ctx.garage.activeGarageVehicleId || ctx.garage.ActiveGarageVehicleId || ctx.garage.activeVehicleId || ctx.garage.ActiveVehicleId);
      select.innerHTML = '';
      var placeholder = document.createElement('option');
      placeholder.value = '';
      placeholder.textContent = vehicles.length ? 'Select vehicle' : 'No vehicles yet — add one';
      select.appendChild(placeholder);

      for (var i = 0; i < vehicles.length; i++) {
        var vehicle = vehicles[i];
        var option = document.createElement('option');
        option.value = String(vehicle.id || vehicle.Id || '');
        option.textContent = vehicle.label || vehicle.Label || ('Vehicle #' + option.value);
        if (activeId != null && String(activeId) === option.value) {
          option.selected = true;
        }
        select.appendChild(option);
      }

      var addOption = document.createElement('option');
      addOption.value = '__add__';
      addOption.textContent = 'Add vehicle (VIN)…';
      select.appendChild(addOption);

      var chip = document.querySelector('.ce-active-vehicle-chip');
      if (chip) {
        var selected = vehicles.filter(function (v) {
          return String(v.id || v.Id) === String(activeId);
        })[0];
        chip.textContent = selected
          ? ('Garage: ' + (selected.label || selected.Label || 'Active vehicle'))
          : (vehicles.length ? 'Garage: select or switch vehicle' : 'Garage empty — add a vehicle to check fitment');
      }
    });

    select.addEventListener('change', function () {
      var value = select.value;
      if (!value) {
        return;
      }
      if (value === '__add__') {
        var vin = window.prompt('Enter VIN to add a vehicle');
        if (!vin) {
          select.value = '';
          return;
        }
        addVehicleByVin(vin.trim()).then(function () {
          populateVehicleSelector();
          evaluateFitmentBand();
        });
        return;
      }
      setActiveVehicle(parseInt(value, 10)).then(function () {
        evaluateFitmentBand();
        populateVehicleSelector();
      });
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
        window.alert('Unable to add vehicle.');
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
          // ignore
        }
      }
    }).catch(function () {
      // optional migration
    });
  }

  function maybeFetchRecommendations() {
    jsonFetch('/check-engine/search/recommend', { method: 'GET' }).then(function (response) {
      if (!response.ok) {
        return;
      }
      return response.json().then(function (payload) {
        var panel = getOrCreateResultsPanel();
        if (!panel.querySelector('.ce-search-results-list')) {
          renderSearchResults(payload);
        }
      });
    }).catch(function () {
      // optional endpoint
    });
  }

  function boot() {
    ensureGuestKey();
    bindStickySearch();
    populateVehicleSelector();
    evaluateFitmentBand();
    maybeMigrateGuestOnLogin();
    maybeFetchRecommendations();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})(window, document);
