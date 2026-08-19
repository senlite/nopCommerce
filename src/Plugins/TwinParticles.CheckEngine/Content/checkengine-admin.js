/* global window, document, fetch */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function token(root) {
    return root.querySelector('input[name="__RequestVerificationToken"]');
  }

  function headers(t, json) {
    var h = { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' };
    if (json) h['Content-Type'] = 'application/json';
    if (t && t.value) h.RequestVerificationToken = t.value;
    return h;
  }

  function apiGet(url, t) {
    return fetch(url, { headers: headers(t), credentials: 'same-origin' }).then(function (r) {
      return r.json().then(function (body) {
        if (!r.ok) throw body;
        return body;
      });
    });
  }

  function showAlert(el, kind, message) {
    if (!el) return;
    el.hidden = !message;
    el.textContent = message || '';
    el.className = 'ce-mp-alert ce-mp-alert--' + (kind || 'info');
  }

  function asArray(data) {
    if (Array.isArray(data)) return data;
    if (data && Array.isArray(data.items)) return data.items;
    if (data && Array.isArray(data.Items)) return data.Items;
    return [];
  }

  function renderRows(tbody, rows, cols, emptyText) {
    if (!tbody) return;
    if (!rows.length) {
      tbody.innerHTML = '<tr><td colspan="' + cols.length + '">' + (emptyText || 'No rows.') + '</td></tr>';
      return;
    }
    tbody.innerHTML = rows
      .map(function (row) {
        return (
          '<tr>' +
          cols
            .map(function (col) {
              var val = col.value(row);
              return '<td' + (col.code ? ' class="ce-code"' : '') + '>' + (val == null ? '—' : val) + '</td>';
            })
            .join('') +
          '</tr>'
        );
      })
      .join('');
  }

  function initVehicleAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-vehicle-body]');
    var select = root.querySelector('[data-ce-vehicle-level]');
    function load() {
      var level = (select && select.value) || 'Makes';
      var url = '/Admin/CheckEngine/VehicleAdmin/' + level + '?json=1';
      showAlert(alert, 'info', 'Loading…');
      apiGet(url, t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          var rows = asArray(data);
          renderRows(tbody, rows, [
            { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'code', 'Code') || pick(r, 'nameEn', 'NameEn') || pick(r, 'name', 'Name'); } },
            { value: function (r) { return pick(r, 'nameAr', 'NameAr'); } },
            { value: function (r) { return pick(r, 'isActive', 'IsActive'); } }
          ]);
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.reasonCode) || 'Failed to load vehicle data.');
        });
    }
    select && select.addEventListener('change', load);
    root.querySelector('[data-ce-vehicle-refresh]')?.addEventListener('click', load);
    load();
  }

  function initOemAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-oem-body]');
    var select = root.querySelector('[data-ce-oem-level]');
    function load() {
      var level = (select && select.value) || 'Manufacturers';
      showAlert(alert, 'info', 'Loading…');
      apiGet('/Admin/CheckEngine/OemAdmin/' + level + '?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          renderRows(tbody, asArray(data), [
            { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'code', 'Code') || pick(r, 'oemNumber', 'OemNumber') || pick(r, 'name', 'Name'); } },
            { value: function (r) { return pick(r, 'manufacturerId', 'ManufacturerId') || pick(r, 'productId', 'ProductId'); }, code: true }
          ]);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Failed to load OEM data.');
        });
    }
    select && select.addEventListener('change', load);
    root.querySelector('[data-ce-oem-refresh]')?.addEventListener('click', load);
    load();
  }

  function initFitmentClaims(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-fitment-body]');
    function load() {
      showAlert(alert, 'info', 'Loading…');
      apiGet('/Admin/CheckEngine/FitmentAdmin/Queue?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          renderRows(tbody, asArray(data), [
            { value: function (r) { return pick(r, 'claimId', 'ClaimId') || pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'productId', 'ProductId'); }, code: true },
            { value: function (r) { return pick(r, 'vehicleConfigurationId', 'VehicleConfigurationId'); }, code: true },
            { value: function (r) { return pick(r, 'status', 'Status') || pick(r, 'fitmentStatus', 'FitmentStatus'); } },
            { value: function (r) { return pick(r, 'source', 'Source'); } }
          ]);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Failed to load fitment queue.');
        });
    }
    root.querySelector('[data-ce-fitment-refresh]')?.addEventListener('click', load);
    load();
  }

  function initGarageAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var panel = root.querySelector('[data-ce-garage-result]');
    root.querySelector('[data-ce-garage-load]')?.addEventListener('click', function () {
      var customerId = Number(root.querySelector('[data-ce-garage-customer]')?.value || 0);
      if (!customerId) {
        showAlert(alert, 'error', 'Enter a customer id.');
        return;
      }
      showAlert(alert, 'info', 'Loading…');
      apiGet('/Admin/CheckEngine/GarageAdmin/CustomerGarage?customerId=' + customerId + '&json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', 'Garage loaded for customer #' + customerId);
          if (panel) panel.textContent = JSON.stringify(data, null, 2);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Garage not found for customer #' + customerId);
          if (panel) panel.textContent = '';
        });
    });
  }

  function initErpAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var panel = root.querySelector('[data-ce-erp-result]');
    root.querySelector('[data-ce-erp-reconcile]')?.addEventListener('click', function () {
      showAlert(alert, 'info', 'Running reconciliation…');
      apiGet('/Admin/CheckEngine/ErpAdmin/Reconcile?json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', 'Reconciliation complete.');
          if (panel) panel.textContent = JSON.stringify(data, null, 2);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Reconciliation failed.');
        });
    });
    root.querySelector('[data-ce-erp-process]')?.addEventListener('click', function () {
      fetch('/Admin/CheckEngine/ErpAdmin/Process', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: '{}'
      })
        .then(function (r) {
          return r.json().then(function (body) {
            if (!r.ok) throw body;
            showAlert(alert, 'success', 'Processed ' + (body.processed || 0) + ' jobs.');
          });
        })
        .catch(function () {
          showAlert(alert, 'error', 'Process failed.');
        });
    });
  }

  function initSeoAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-seo-body]');
    function load() {
      showAlert(alert, 'info', 'Loading sitemap…');
      apiGet('/Admin/CheckEngine/SeoAdmin/Sitemap?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          var rows = asArray(data);
          renderRows(tbody, rows, [
            { value: function (r) { return typeof r === 'string' ? r : pick(r, 'url', 'Url') || pick(r, 'loc', 'Loc'); } }
          ]);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Failed to load sitemap URLs.');
        });
    }
    root.querySelector('[data-ce-seo-refresh]')?.addEventListener('click', load);
    root.querySelector('[data-ce-seo-rebuild]')?.addEventListener('click', function () {
      fetch('/Admin/CheckEngine/SeoAdmin/RebuildSitemap', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: '{}'
      })
        .then(function (r) {
          if (!r.ok) throw new Error('fail');
          showAlert(alert, 'success', 'Sitemap rebuild queued.');
          load();
        })
        .catch(function () {
          showAlert(alert, 'error', 'Rebuild failed.');
        });
    });
    load();
  }

  function initImportBatch(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var panel = root.querySelector('[data-ce-import-result]');
    root.querySelector('[data-ce-import-load]')?.addEventListener('click', function () {
      var batchId = root.querySelector('[data-ce-import-batch]')?.value || '';
      if (!batchId.trim()) {
        showAlert(alert, 'error', 'Enter a batch id.');
        return;
      }
      showAlert(alert, 'info', 'Loading batch…');
      apiGet('/Admin/CheckEngine/ImportAdmin/Batch?batchId=' + encodeURIComponent(batchId.trim()) + '&json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', 'Batch loaded.');
          if (panel) panel.textContent = JSON.stringify(data, null, 2);
        })
        .catch(function () {
          showAlert(alert, 'error', 'Batch not found.');
          if (panel) panel.textContent = '';
        });
    });
  }

  function initDiagnosticsAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-diagnostics-stats]');
    function load() {
      showAlert(alert, 'info', 'Loading diagnostics…');
      apiGet('/Admin/CheckEngine/DiagnosticsAdmin/Package?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          var health = pick(data, 'health', 'Health') || {};
          if (stats) {
            stats.innerHTML =
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Database</span><span class="ce-mp-stat__value">' + (pick(health, 'database', 'Database') || '—') + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Search</span><span class="ce-mp-stat__value">' + (pick(health, 'searchIndex', 'SearchIndex') || '—') + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">ERP</span><span class="ce-mp-stat__value">' + (pick(health, 'erp', 'Erp') || '—') + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Licence</span><span class="ce-mp-stat__value">' + (pick(health, 'licence', 'Licence') || '—') + '</span></article>';
          }
        })
        .catch(function () {
          showAlert(alert, 'error', 'Failed to load diagnostics package.');
        });
    }
    root.querySelector('[data-ce-diagnostics-refresh]')?.addEventListener('click', load);
    load();
  }

  function initReferenceAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-reference-stats]');
    var panel = root.querySelector('[data-ce-reference-result]');
    function load() {
      apiGet('/Admin/CheckEngine/ReferenceDataAdmin/Status?json=1', t)
        .then(function (data) {
          var status = pick(data, 'status', 'Status') || {};
          if (stats) {
            stats.innerHTML =
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Products</span><span class="ce-mp-stat__value">' + (pick(status, 'productCount', 'ProductCount') || 0) + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Fitment</span><span class="ce-mp-stat__value">' + (pick(status, 'fitmentClaimCount', 'FitmentClaimCount') || 0) + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">Configs</span><span class="ce-mp-stat__value">' + (pick(status, 'configurationCount', 'ConfigurationCount') || 0) + '</span></article>' +
              '<article class="ce-mp-stat"><span class="ce-mp-stat__label">OEM</span><span class="ce-mp-stat__value">' + (pick(status, 'oemEntryCount', 'OemEntryCount') || 0) + '</span></article>';
          }
        })
        .catch(function () {
          showAlert(alert, 'error', 'Failed to load reference catalog status.');
        });
    }
    root.querySelector('[data-ce-reference-refresh]')?.addEventListener('click', load);
    root.querySelector('[data-ce-reference-load]')?.addEventListener('click', function () {
      var scale = parseFloat(root.querySelector('[data-ce-reference-scale]')?.value || '1');
      var replace = !!root.querySelector('[data-ce-reference-replace]')?.checked;
      showAlert(alert, 'info', 'Loading catalog…');
      fetch('/Admin/CheckEngine/ReferenceDataAdmin/Load', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({ scaleFactor: scale, replaceExisting: replace, ensureBmwVehicleSeed: true })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (panel) panel.textContent = JSON.stringify(res.body, null, 2);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? 'Catalog load complete.' : 'Catalog load failed.');
          load();
        })
        .catch(function () {
          showAlert(alert, 'error', 'Catalog load failed.');
        });
    });
    root.querySelector('[data-ce-reference-purge]')?.addEventListener('click', function () {
      if (!window.confirm('Purge the reference-scale catalog?')) return;
      fetch('/Admin/CheckEngine/ReferenceDataAdmin/Purge', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: '{}'
      })
        .then(function (r) {
          showAlert(alert, r.ok ? 'success' : 'error', r.ok ? 'Catalog purged.' : 'Purge failed.');
          load();
        })
        .catch(function () {
          showAlert(alert, 'error', 'Purge failed.');
        });
    });
    load();
  }

  function initImageAdmin(root) {
    var t = token(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var panel = root.querySelector('[data-ce-image-result]');
    root.querySelector('[data-ce-image-replace]')?.addEventListener('click', function () {
      var productId = parseInt(root.querySelector('[data-ce-image-product]')?.value || '0', 10);
      var sourceUrl = root.querySelector('[data-ce-image-url]')?.value || '';
      if (!productId || !sourceUrl.trim()) {
        showAlert(alert, 'error', 'Enter a product id and source URL.');
        return;
      }
      showAlert(alert, 'info', 'Replacing image…');
      fetch('/Admin/CheckEngine/ImageAdmin/Replace', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({
          productId: productId,
          sourceUrl: sourceUrl.trim(),
          seoName: root.querySelector('[data-ce-image-seo]')?.value || ''
        })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (panel) panel.textContent = JSON.stringify(res.body, null, 2);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? 'Primary image replaced.' : 'Replace failed.');
        })
        .catch(function () {
          showAlert(alert, 'error', 'Replace failed.');
        });
    });
    root.querySelector('[data-ce-image-manifest-submit]')?.addEventListener('click', function () {
      var csv = root.querySelector('[data-ce-image-manifest]')?.value || '';
      if (!csv.trim()) {
        showAlert(alert, 'error', 'Paste a CSV manifest.');
        return;
      }
      fetch('/Admin/CheckEngine/ImageAdmin/SourceFromManifest', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({ csvContent: csv })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (panel) panel.textContent = JSON.stringify(res.body, null, 2);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? 'Manifest applied.' : 'Manifest failed.');
        })
        .catch(function () {
          showAlert(alert, 'error', 'Manifest failed.');
        });
    });
  }

  function boot() {
    document.querySelectorAll('[data-ce-page="vehicle-admin"]').forEach(initVehicleAdmin);
    document.querySelectorAll('[data-ce-page="oem-admin"]').forEach(initOemAdmin);
    document.querySelectorAll('[data-ce-page="fitment-claims"]').forEach(initFitmentClaims);
    document.querySelectorAll('[data-ce-page="garage-admin"]').forEach(initGarageAdmin);
    document.querySelectorAll('[data-ce-page="erp-admin"]').forEach(initErpAdmin);
    document.querySelectorAll('[data-ce-page="seo-admin"]').forEach(initSeoAdmin);
    document.querySelectorAll('[data-ce-page="import-batch"]').forEach(initImportBatch);
    document.querySelectorAll('[data-ce-page="diagnostics-admin"]').forEach(initDiagnosticsAdmin);
    document.querySelectorAll('[data-ce-page="reference-admin"]').forEach(initReferenceAdmin);
    document.querySelectorAll('[data-ce-page="image-admin"]').forEach(initImageAdmin);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})(window);
