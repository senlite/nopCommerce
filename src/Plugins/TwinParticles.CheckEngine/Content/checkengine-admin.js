/* global window, document, fetch */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function readI18n(root) {
    if (window.CheckEngineAdmin && typeof CheckEngineAdmin.readI18n === 'function')
      return CheckEngineAdmin.readI18n(root);
    var node = root && root.querySelector('[data-ce-i18n]');
    if (!node) return {};
    try { return JSON.parse(node.textContent || '{}'); } catch (e) { return {}; }
  }

  function fmt(template) {
    if (window.CheckEngineAdmin && typeof CheckEngineAdmin.format === 'function')
      return CheckEngineAdmin.format.apply(null, arguments);
    var args = Array.prototype.slice.call(arguments, 1);
    return String(template == null ? '' : template).replace(/\{(\d+)\}/g, function (_, i) {
      return args[i] != null ? String(args[i]) : '';
    });
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

  function errorMessage(err, fallback) {
    var code = '';
    if (typeof err === 'string') code = err;
    else if (err) code = pick(err, 'reasonCode', 'ReasonCode') || pick(err, 'errorCode', 'ErrorCode') || '';
    if (code === 'licence.read_only')
      return 'Check Engine is in licence read-only mode. Activate a licence on the dashboard to make changes.';
    return code || fallback || 'Request failed.';
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

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function display(value) {
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return escapeHtml(value);
  }

  function renderStatGrid(el, items) {
    if (!el) return;
    el.innerHTML = (items || [])
      .map(function (item) {
        return (
          '<article class="ce-mp-stat"><span class="ce-mp-stat__label">' +
          escapeHtml(item.label) +
          '</span><span class="ce-mp-stat__value' +
          (item.code ? ' ce-code' : '') +
          '">' +
          display(item.value) +
          '</span></article>'
        );
      })
      .join('');
  }

  function imageStatusLabel(value, i18n) {
    i18n = i18n || {};
    var map = {
      1: i18n.statReplaced || 'Replaced',
      2: i18n.statNotFound || 'Not found',
      3: i18n.statQuarantined || 'Quarantined',
      4: i18n.statFailed || 'Failed',
      Replaced: i18n.statReplaced || 'Replaced',
      SkuNotFound: i18n.statNotFound || 'Not found',
      Quarantined: i18n.statQuarantined || 'Quarantined',
      Failed: i18n.statFailed || 'Failed'
    };
    return map[value] || value;
  }

  function renderBatchImageResult(statsEl, rowsEl, body, i18n) {
    i18n = i18n || {};
    renderStatGrid(statsEl, [
      { label: i18n.statReplaced || 'Replaced', value: pick(body, 'replaced', 'Replaced') },
      { label: i18n.statNotFound || 'Not found', value: pick(body, 'notFound', 'NotFound') },
      { label: i18n.statQuarantined || 'Quarantined', value: pick(body, 'quarantined', 'Quarantined') },
      { label: i18n.statFailed || 'Failed', value: pick(body, 'failed', 'Failed') }
    ]);
    renderRows(
      rowsEl,
      asArray(pick(body, 'items', 'Items')),
      [
        { value: function (r) { return pick(r, 'sku', 'Sku'); }, code: true },
        { value: function (r) { return pick(r, 'productId', 'ProductId'); }, code: true },
        { value: function (r) { return imageStatusLabel(pick(r, 'status', 'Status'), i18n); } },
        { value: function (r) { return pick(r, 'errorCode', 'ErrorCode'); } }
      ],
      i18n.noRows || 'No rows.'
    );
  }

  function renderRows(tbody, rows, cols, emptyText) {
    if (!tbody) return;
    if (!rows.length) {
      tbody.innerHTML = '<tr><td colspan="' + cols.length + '">' + escapeHtml(emptyText || 'No rows.') + '</td></tr>';
      return;
    }
    tbody.innerHTML = rows
      .map(function (row) {
        return (
          '<tr>' +
          cols
            .map(function (col) {
              var cell = col.html ? col.value(row) : display(col.value(row));
              return '<td' + (col.code ? ' class="ce-code"' : '') + '>' + cell + '</td>';
            })
            .join('') +
          '</tr>'
        );
      })
      .join('');
  }

  function initVehicleAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-vehicle-body]');
    var select = root.querySelector('[data-ce-vehicle-level]');
    function load(doneKind, doneMessage) {
      var level = (select && select.value) || 'Makes';
      var url = '/Admin/CheckEngine/VehicleAdmin/' + level + '?json=1';
      if (!doneMessage) showAlert(alert, 'info', i18n.loading || 'Loading…');
      return apiGet(url, t)
        .then(function (data) {
          var rows = asArray(data);
          renderRows(tbody, rows, [
            { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'code', 'Code') || pick(r, 'nameEn', 'NameEn') || pick(r, 'name', 'Name'); } },
            { value: function (r) { return pick(r, 'nameAr', 'NameAr'); } },
            { value: function (r) { return pick(r, 'isActive', 'IsActive'); } }
          ], i18n.noRows || 'No rows.');
          showAlert(alert, doneKind || 'info', doneMessage || '');
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.loadFailed || 'Failed to load vehicle data.'));
        });
    }
    select && select.addEventListener('change', load);
    root.querySelector('[data-ce-vehicle-refresh]')?.addEventListener('click', load);
    var seedBtn = root.querySelector('[data-ce-vehicle-seed]');
    seedBtn && seedBtn.addEventListener('click', function () {
      seedBtn.disabled = true;
      showAlert(alert, 'info', i18n.seeding || 'Seeding BMW reference data…');
      fetch('/Admin/CheckEngine/VehicleAdmin/Seed', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: '{}'
      })
        .then(function (r) {
          return r.json().then(function (body) {
            return { ok: r.ok, body: body };
          });
        })
        .then(function (res) {
          var body = res.body || {};
          if (!res.ok) {
            showAlert(alert, 'error', errorMessage(body, i18n.seedFailed || 'BMW seed failed.'));
            return;
          }
          var makes = pick(body, 'makesInserted', 'MakesInserted') || 0;
          var configs = pick(body, 'configurationsInserted', 'ConfigurationsInserted') || 0;
          var aliases = pick(body, 'aliasesInserted', 'AliasesInserted') || 0;
          return load(
            'success',
            fmt(i18n.seedComplete || 'BMW seed complete. Makes +{0}, configurations +{1}, aliases +{2}.', makes, configs, aliases)
          );
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.seedFailed || 'BMW seed failed.'));
        })
        .then(function () {
          seedBtn.disabled = false;
        });
    });
    load();
  }

  function initOemAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-oem-body]');
    var select = root.querySelector('[data-ce-oem-level]');
    function load() {
      var level = (select && select.value) || 'Manufacturers';
      showAlert(alert, 'info', i18n.loading || 'Loading…');
      apiGet('/Admin/CheckEngine/OemAdmin/' + level + '?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          renderRows(tbody, asArray(data), [
            { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'code', 'Code') || pick(r, 'oemNumber', 'OemNumber') || pick(r, 'name', 'Name'); } },
            { value: function (r) { return pick(r, 'manufacturerId', 'ManufacturerId') || pick(r, 'productId', 'ProductId'); }, code: true }
          ], i18n.noRows || 'No rows.');
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.loadFailed || 'Failed to load OEM data.');
        });
    }
    select && select.addEventListener('change', load);
    root.querySelector('[data-ce-oem-refresh]')?.addEventListener('click', load);
    load();
  }

  function initFitmentClaims(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-fitment-body]');
    function load() {
      showAlert(alert, 'info', i18n.loading || 'Loading…');
      apiGet('/Admin/CheckEngine/FitmentAdmin/Queue?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          renderRows(tbody, asArray(data), [
            { value: function (r) { return pick(r, 'claimId', 'ClaimId') || pick(r, 'id', 'Id'); }, code: true },
            { value: function (r) { return pick(r, 'productId', 'ProductId'); }, code: true },
            { value: function (r) { return pick(r, 'vehicleConfigurationId', 'VehicleConfigurationId'); }, code: true },
            { value: function (r) { return pick(r, 'status', 'Status') || pick(r, 'fitmentStatus', 'FitmentStatus'); } },
            { value: function (r) { return pick(r, 'source', 'Source'); } }
          ], i18n.noRows || 'No rows.');
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.loadFailed || 'Failed to load fitment queue.');
        });
    }
    root.querySelector('[data-ce-fitment-refresh]')?.addEventListener('click', load);
    load();
  }

  function initGarageAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-garage-stats]');
    var vehiclesBody = root.querySelector('[data-ce-garage-vehicles]');
    var oemsBody = root.querySelector('[data-ce-garage-oems]');
    var vehicleCols = [
      { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
      { value: function (r) { return pick(r, 'vin', 'Vin'); }, code: true },
      { value: function (r) { return pick(r, 'label', 'Label'); } },
      { value: function (r) { return pick(r, 'vehicleConfigurationId', 'VehicleConfigurationId'); }, code: true },
      { value: function (r) { return pick(r, 'isActive', 'IsActive'); } }
    ];
    var oemCols = [
      { value: function (r) { return pick(r, 'id', 'Id'); }, code: true },
      { value: function (r) { return pick(r, 'displayNumber', 'DisplayNumber') || pick(r, 'oemNumberId', 'OemNumberId'); }, code: true }
    ];
    function clearGarage() {
      if (stats) stats.innerHTML = '';
      renderRows(vehiclesBody, [], vehicleCols, i18n.emptyVehicles || 'No vehicles.');
      renderRows(oemsBody, [], oemCols, i18n.emptyOems || 'No OEM numbers.');
    }
    root.querySelector('[data-ce-garage-load]')?.addEventListener('click', function () {
      var customerId = Number(root.querySelector('[data-ce-garage-customer]')?.value || 0);
      if (!customerId) {
        showAlert(alert, 'error', i18n.needCustomer || 'Enter a customer id.');
        return;
      }
      showAlert(alert, 'info', i18n.loading || 'Loading…');
      apiGet('/Admin/CheckEngine/GarageAdmin/CustomerGarage?customerId=' + customerId + '&json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', fmt(i18n.loaded || 'Garage loaded for customer #{0}.', customerId));
          var vehicles = asArray(pick(data, 'vehicles', 'Vehicles'));
          var oems = asArray(pick(data, 'oems', 'Oems'));
          renderStatGrid(stats, [
            { label: i18n.statGarageId || 'Garage id', value: pick(data, 'id', 'Id'), code: true },
            { label: i18n.statCustomer || 'Customer', value: pick(data, 'customerId', 'CustomerId') || customerId, code: true },
            { label: i18n.statVehicles || 'Vehicles', value: vehicles.length },
            { label: i18n.statOems || 'OEM numbers', value: oems.length },
            { label: i18n.statActiveVehicle || 'Active vehicle', value: pick(data, 'activeGarageVehicleId', 'ActiveGarageVehicleId'), code: true }
          ]);
          renderRows(vehiclesBody, vehicles, vehicleCols, i18n.emptyVehicles || 'No vehicles.');
          renderRows(oemsBody, oems, oemCols, i18n.emptyOems || 'No OEM numbers.');
        })
        .catch(function () {
          showAlert(alert, 'error', fmt(i18n.notFound || 'Garage not found for customer #{0}', customerId));
          clearGarage();
        });
    });
  }

  function initErpAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-erp-stats]');
    var variancesBody = root.querySelector('[data-ce-erp-variances]');
    var issuesEl = root.querySelector('[data-ce-erp-issues]');
    root.querySelector('[data-ce-erp-reconcile]')?.addEventListener('click', function () {
      showAlert(alert, 'info', i18n.reconciling || 'Running reconciliation…');
      apiGet('/Admin/CheckEngine/ErpAdmin/Reconcile?json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', i18n.reconcileComplete || 'Reconciliation complete.');
          renderStatGrid(stats, [
            { label: i18n.statTotalJobs || 'Total jobs', value: pick(data, 'totalJobs', 'TotalJobs') },
            { label: i18n.statSuccessful || 'Successful', value: pick(data, 'successfulJobs', 'SuccessfulJobs') },
            { label: i18n.statFailed || 'Failed', value: pick(data, 'failedJobs', 'FailedJobs') },
            { label: i18n.statDiscrepancy || 'Discrepancy', value: pick(data, 'hasFinancialDiscrepancy', 'HasFinancialDiscrepancy') },
            { label: i18n.statGenerated || 'Generated', value: pick(data, 'generatedUtc', 'GeneratedUtc') }
          ]);
          renderRows(
            variancesBody,
            asArray(pick(data, 'variances', 'Variances')),
            [
              { value: function (r) { return pick(r, 'metric', 'Metric'); } },
              { value: function (r) { return pick(r, 'local', 'Local'); } },
              { value: function (r) { return pick(r, 'erp', 'Erp'); } },
              { value: function (r) { return pick(r, 'absoluteVariance', 'AbsoluteVariance'); } },
              { value: function (r) { return pick(r, 'withinTolerance', 'WithinTolerance'); } }
            ],
            i18n.emptyVariances || 'No variances.'
          );
          var issues = asArray(pick(data, 'issues', 'Issues'));
          if (issuesEl) {
            issuesEl.innerHTML = issues.length
              ? issues.map(function (issue) { return '<li>' + escapeHtml(issue) + '</li>'; }).join('')
              : '<li class="text-muted">' + escapeHtml(i18n.emptyIssues || 'No issues.') + '</li>';
          }
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.reconcileFailed || 'Reconciliation failed.');
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
            showAlert(alert, 'success', fmt(i18n.processed || 'Processed {0} jobs.', pick(body, 'processed', 'Processed') || 0));
          });
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.processFailed || 'Process failed.'));
        });
    });
    root.querySelector('[data-ce-erp-snapshot]')?.addEventListener('click', function () {
      showAlert(alert, 'info', i18n.snapshotLoading || 'Pulling inventory snapshot…');
      var snapshotEl = root.querySelector('[data-ce-erp-snapshot-stats]');
      apiGet('/Admin/CheckEngine/ErpAdmin/InventorySnapshot?json=1', t)
        .then(function (data) {
          var payload = pick(data, 'payload', 'Payload');
          var parsed = null;
          if (payload && typeof payload === 'object') parsed = payload;
          else if (typeof payload === 'string') {
            try { parsed = JSON.parse(payload); } catch (e) { parsed = null; }
          }
          var items = parsed ? asArray(pick(parsed, 'items', 'Items')) : [];
          renderStatGrid(snapshotEl, [
            { label: i18n.statWarehouse || 'Warehouse', value: parsed ? pick(parsed, 'warehouse', 'Warehouse') : '—' },
            { label: i18n.statItems || 'Items', value: items.length },
            { label: i18n.statPayload || 'Payload', value: parsed ? (i18n.payloadStructured || 'Structured') : (payload ? String(payload).slice(0, 80) : (i18n.payloadEmpty || 'Empty')) }
          ]);
          showAlert(alert, 'success', i18n.snapshotLoaded || 'Inventory snapshot loaded.');
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.snapshotFailed || 'Inventory snapshot failed.');
        });
    });
  }

  function initSeoAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var tbody = root.querySelector('[data-ce-seo-body]');
    function load() {
      showAlert(alert, 'info', i18n.loading || 'Loading sitemap…');
      apiGet('/Admin/CheckEngine/SeoAdmin/Sitemap?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          var rows = asArray(data);
          renderRows(tbody, rows, [
            { value: function (r) { return typeof r === 'string' ? r : pick(r, 'url', 'Url') || pick(r, 'loc', 'Loc'); } }
          ], i18n.noRows || 'No rows.');
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.loadFailed || 'Failed to load sitemap URLs.');
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
          return r.json().then(function (body) { return { ok: r.ok, body: body }; }).catch(function () { return { ok: r.ok, body: {} }; });
        })
        .then(function (res) {
          if (!res.ok) throw res.body;
          showAlert(alert, 'success', i18n.rebuildQueued || 'Sitemap rebuild queued.');
          load();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.rebuildFailed || 'Rebuild failed.'));
        });
    });
    function renderLanding(body) {
      var landing = pick(body, 'landing', 'Landing') || {};
      renderStatGrid(root.querySelector('[data-ce-seo-result]'), [
        { label: i18n.statSuccess || 'Success', value: pick(body, 'success', 'Success') },
        { label: i18n.statId || 'Id', value: pick(landing, 'id', 'Id'), code: true },
        { label: i18n.statType || 'Type', value: pick(landing, 'type', 'Type') },
        { label: i18n.statLocale || 'Locale', value: pick(landing, 'locale', 'Locale') },
        { label: i18n.statUrl || 'URL', value: pick(landing, 'urlPath', 'UrlPath') },
        { label: i18n.statIndexable || 'Indexable', value: pick(landing, 'isIndexable', 'IsIndexable') },
        { label: i18n.statError || 'Error', value: pick(body, 'errorCode', 'ErrorCode') }
      ]);
    }
    function generate(kind) {
      var configId = parseInt(root.querySelector('[data-ce-seo-config]')?.value || '0', 10);
      var productId = parseInt(root.querySelector('[data-ce-seo-product]')?.value || '0', 10);
      var locale = root.querySelector('[data-ce-seo-locale]')?.value || 'en';
      if (!configId) {
        showAlert(alert, 'error', i18n.needConfig || 'Enter a configuration id.');
        return;
      }
      if (kind === 'part' && !productId) {
        showAlert(alert, 'error', i18n.needProduct || 'Enter a product id for a part landing.');
        return;
      }
      var url = kind === 'part'
        ? '/Admin/CheckEngine/SeoAdmin/GeneratePartForVehicle'
        : '/Admin/CheckEngine/SeoAdmin/GenerateVehicle';
      var payload = kind === 'part'
        ? { productId: productId, vehicleConfigurationId: configId, locale: locale }
        : { vehicleConfigurationId: configId, locale: locale };
      showAlert(alert, 'info', i18n.generating || 'Generating landing…');
      fetch(url, {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify(payload)
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          renderLanding(res.body || {});
          showAlert(alert, res.ok && pick(res.body, 'success', 'Success') !== false ? 'success' : 'error',
            res.ok ? (i18n.generated || 'Landing generated.') : errorMessage(res.body, i18n.generateFailed || 'Generate failed.'));
          load();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.generateFailed || 'Generate failed.'));
        });
    }
    root.querySelector('[data-ce-seo-generate-vehicle]')?.addEventListener('click', function () { generate('vehicle'); });
    root.querySelector('[data-ce-seo-generate-part]')?.addEventListener('click', function () { generate('part'); });
    load();
  }

  function initImportBatch(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-import-stats]');
    var rowsBody = root.querySelector('[data-ce-import-rows]');
    var rowCols = [
      { value: function (r) { return pick(r, 'rowNumber', 'RowNumber'); } },
      { value: function (r) { return pick(r, 'reviewStatus', 'ReviewStatus'); } },
      { value: function (r) { return pick(r, 'oemNumberNormalized', 'OemNumberNormalized'); }, code: true },
      { value: function (r) { return pick(r, 'vehicleConfigurationId', 'VehicleConfigurationId'); }, code: true },
      { value: function (r) { return pick(r, 'isPublished', 'IsPublished'); } },
      { value: function (r) { return pick(r, 'lastStageError', 'LastStageError') || pick(r, 'publishError', 'PublishError'); } },
      { html: true, value: function (r) {
          var n = pick(r, 'rowNumber', 'RowNumber');
          var html = '<button type="button" class="btn btn-xs btn-success" data-ce-import-review="Approved" data-row="' + n + '">' + (i18n.approve || 'Approve') + '</button> ' +
            '<button type="button" class="btn btn-xs btn-danger" data-ce-import-review="Rejected" data-row="' + n + '">' + (i18n.reject || 'Reject') + '</button>';
          if (pick(r, 'isDuplicate', 'IsDuplicate')) {
            html += ' <button type="button" class="btn btn-xs btn-secondary" data-ce-import-decision="Merge" data-row="' + n + '">' + (i18n.merge || 'Merge') + '</button>' +
              ' <button type="button" class="btn btn-xs btn-secondary" data-ce-import-decision="Link" data-row="' + n + '">' + (i18n.link || 'Link') + '</button>' +
              ' <button type="button" class="btn btn-xs btn-secondary" data-ce-import-decision="KeepSeparate" data-row="' + n + '">' + (i18n.keep || 'Keep') + '</button>';
          }
          return html;
        } }
    ];
    function loadBatch() {
      var batchId = root.querySelector('[data-ce-import-batch]')?.value || '';
      if (!batchId.trim()) {
        showAlert(alert, 'error', i18n.needId || 'Enter a batch id.');
        return;
      }
      showAlert(alert, 'info', i18n.loading || 'Loading batch…');
      apiGet('/Admin/CheckEngine/ImportAdmin/Batch?batchId=' + encodeURIComponent(batchId.trim()) + '&json=1', t)
        .then(function (data) {
          showAlert(alert, 'success', i18n.loaded || 'Batch loaded.');
          renderStatGrid(stats, [
            { label: i18n.batchId || 'Batch id', value: pick(data, 'batchId', 'BatchId'), code: true },
            { label: i18n.file || 'File', value: pick(data, 'fileName', 'FileName') },
            { label: i18n.status || 'Status', value: pick(data, 'status', 'Status') },
            { label: i18n.stage || 'Stage', value: pick(data, 'currentStage', 'CurrentStage') },
            { label: i18n.totalRows || 'Total rows', value: pick(data, 'totalRows', 'TotalRows') },
            { label: i18n.published || 'Published', value: pick(data, 'publishedRows', 'PublishedRows') },
            { label: i18n.failed || 'Failed', value: pick(data, 'failedRows', 'FailedRows') }
          ]);
          renderRows(rowsBody, asArray(pick(data, 'rows', 'Rows')), rowCols, i18n.noRows || 'No rows.');
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.notFound || 'Batch not found.');
          if (stats) stats.innerHTML = '';
          renderRows(rowsBody, [], rowCols, i18n.noRows || 'No rows.');
        });
    }
    function currentBatchId() {
      return (root.querySelector('[data-ce-import-batch]')?.value || '').trim();
    }
    function postRowAction(url, payload, okText) {
      var batchId = currentBatchId();
      if (!batchId) {
        showAlert(alert, 'error', i18n.needId || 'Enter a batch id.');
        return;
      }
      fetch(url, {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify(Object.assign({ batchId: batchId }, payload))
      })
        .then(function (r) {
          return r.json().then(function (body) { return { ok: r.ok, body: body }; }).catch(function () { return { ok: r.ok, body: {} }; });
        })
        .then(function (res) {
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? okText : errorMessage(res.body, 'Action failed.'));
          if (res.ok) loadBatch();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, 'Action failed.'));
        });
    }
    if (rowsBody) {
      rowsBody.addEventListener('click', function (ev) {
        var btn = ev.target.closest('[data-ce-import-review],[data-ce-import-decision]');
        if (!btn) return;
        var rowNumber = parseInt(btn.getAttribute('data-row') || '0', 10);
        if (btn.hasAttribute('data-ce-import-review')) {
          postRowAction('/Admin/CheckEngine/ImportAdmin/SetReviewStatus', {
            rowNumber: rowNumber,
            reviewStatus: btn.getAttribute('data-ce-import-review')
          }, i18n.reviewUpdated || 'Review updated.');
        } else {
          postRowAction('/Admin/CheckEngine/ImportAdmin/SetDuplicateDecision', {
            rowNumber: rowNumber,
            decision: btn.getAttribute('data-ce-import-decision')
          }, i18n.decisionSaved || 'Duplicate decision saved.');
        }
      });
    }
    root.querySelector('[data-ce-import-load]')?.addEventListener('click', loadBatch);
    root.querySelector('[data-ce-import-run]')?.addEventListener('click', function () {
      var file = root.querySelector('[data-ce-import-file]')?.files?.[0];
      if (!file) {
        showAlert(alert, 'error', i18n.chooseFile || 'Choose a catalog file.');
        return;
      }
      var format = parseInt(root.querySelector('[data-ce-import-format]')?.value || '1', 10);
      var dryRun = !!root.querySelector('[data-ce-import-run-dry-run]')?.checked;
      showAlert(alert, 'info', i18n.running || 'Running import…');
      var reader = new FileReader();
      reader.onload = function () {
        var bytes = new Uint8Array(reader.result);
        var binary = '';
        for (var i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
        fetch('/Admin/CheckEngine/ImportAdmin/Run', {
          method: 'POST',
          headers: headers(t, true),
          credentials: 'same-origin',
          body: JSON.stringify({
            format: format,
            fileName: file.name,
            contentBase64: btoa(binary),
            dryRun: dryRun
          })
        })
          .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
          .then(function (res) {
            var id = pick(res.body, 'batchId', 'BatchId');
            if (id && root.querySelector('[data-ce-import-batch]')) {
              root.querySelector('[data-ce-import-batch]').value = id;
            }
            showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.started || 'Import pipeline started.') : errorMessage(res.body, i18n.importFailed || 'Import failed.'));
            if (res.ok && id) loadBatch();
          })
          .catch(function (err) {
            showAlert(alert, 'error', errorMessage(err, i18n.importFailed || 'Import failed.'));
          });
      };
      reader.readAsArrayBuffer(file);
    });
    root.querySelector('[data-ce-import-publish]')?.addEventListener('click', function () {
      var batchId = root.querySelector('[data-ce-import-batch]')?.value || '';
      if (!batchId.trim()) {
        showAlert(alert, 'error', i18n.needId || 'Enter a batch id.');
        return;
      }
      var dryRun = !!root.querySelector('[data-ce-import-dry-run]')?.checked;
      showAlert(alert, 'info', dryRun ? (i18n.publishingDryRun || 'Publishing (dry run)…') : (i18n.publishing || 'Publishing…'));
      fetch('/Admin/CheckEngine/ImportAdmin/Publish', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({ batchId: batchId.trim(), dryRun: dryRun })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          renderStatGrid(root.querySelector('[data-ce-import-publish-stats]'), [
            { label: i18n.published || 'Published', value: pick(res.body, 'publishedRows', 'PublishedRows') },
            { label: i18n.failed || 'Failed', value: pick(res.body, 'failedRows', 'FailedRows') },
            { label: i18n.dryRun || 'Dry run', value: pick(res.body, 'dryRun', 'DryRun') }
          ]);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.publishComplete || 'Publish complete.') : errorMessage(res.body, i18n.publishFailed || 'Publish failed.'));
          loadBatch();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, 'Publish failed.'));
        });
    });
    var initial = root.querySelector('[data-ce-import-batch]')?.value;
    if (initial && initial.trim()) loadBatch();
  }

  function initDiagnosticsAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-diagnostics-stats]');
    function load() {
      showAlert(alert, 'info', i18n.loading || 'Loading diagnostics…');
      apiGet('/Admin/CheckEngine/DiagnosticsAdmin/Package?json=1', t)
        .then(function (data) {
          showAlert(alert, 'info', '');
          var health = pick(data, 'health', 'Health') || {};
          renderStatGrid(stats, [
            { label: i18n.statDatabase || 'Database', value: pick(health, 'database', 'Database') },
            { label: i18n.statSearch || 'Search', value: pick(health, 'searchIndex', 'SearchIndex') },
            { label: i18n.statErp || 'ERP', value: pick(health, 'erp', 'Erp') },
            { label: i18n.statLicence || 'Licence', value: pick(health, 'licence', 'Licence') }
          ]);
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.loadFailed || 'Failed to load diagnostics package.');
        });
    }
    root.querySelector('[data-ce-diagnostics-refresh]')?.addEventListener('click', load);
    load();
  }

  function initReferenceAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var stats = root.querySelector('[data-ce-reference-stats]');
    var panel = root.querySelector('[data-ce-reference-result]');
    function load() {
      apiGet('/Admin/CheckEngine/ReferenceDataAdmin/Status?json=1', t)
        .then(function (data) {
          var status = pick(data, 'status', 'Status') || {};
          renderStatGrid(stats, [
            { label: i18n.statProducts || 'Products', value: pick(status, 'productCount', 'ProductCount') || 0 },
            { label: i18n.statFitment || 'Fitment', value: pick(status, 'fitmentClaimCount', 'FitmentClaimCount') || 0 },
            { label: i18n.statConfigs || 'Configs', value: pick(status, 'configurationCount', 'ConfigurationCount') || 0 },
            { label: i18n.statOem || 'OEM', value: pick(status, 'oemEntryCount', 'OemEntryCount') || 0 }
          ]);
        })
        .catch(function () {
          showAlert(alert, 'error', i18n.statusFailed || 'Failed to load reference catalog status.');
        });
    }
    root.querySelector('[data-ce-reference-refresh]')?.addEventListener('click', load);
    root.querySelector('[data-ce-reference-load]')?.addEventListener('click', function () {
      var scale = parseFloat(root.querySelector('[data-ce-reference-scale]')?.value || '1');
      var replace = !!root.querySelector('[data-ce-reference-replace]')?.checked;
      showAlert(alert, 'info', i18n.loading || 'Loading catalog…');
      fetch('/Admin/CheckEngine/ReferenceDataAdmin/Load', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({ scaleFactor: scale, replaceExisting: replace, ensureBmwVehicleSeed: true })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          renderStatGrid(panel, [
            { label: i18n.statAlreadyLoaded || 'Already loaded', value: pick(res.body, 'alreadyLoaded', 'AlreadyLoaded') },
            { label: i18n.statProducts || 'Products', value: pick(res.body, 'productsInserted', 'ProductsInserted') },
            { label: i18n.statFitmentClaims || 'Fitment claims', value: pick(res.body, 'fitmentClaimsInserted', 'FitmentClaimsInserted') },
            { label: i18n.statConfigurations || 'Configurations', value: pick(res.body, 'configurationsInserted', 'ConfigurationsInserted') },
            { label: i18n.statOemEntries || 'OEM entries', value: pick(res.body, 'oemEntriesUpserted', 'OemEntriesUpserted') }
          ]);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.loadComplete || 'Catalog load complete.') : errorMessage(res.body, i18n.loadFailed || 'Catalog load failed.'));
          load();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.loadFailed || 'Catalog load failed.'));
        });
    });
    root.querySelector('[data-ce-reference-purge]')?.addEventListener('click', function () {
      if (!window.confirm(i18n.purgeConfirm || 'Purge the reference-scale catalog?')) return;
      fetch('/Admin/CheckEngine/ReferenceDataAdmin/Purge', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: '{}'
      })
        .then(function (r) {
          return r.json().then(function (body) { return { ok: r.ok, body: body }; }).catch(function () { return { ok: r.ok, body: {} }; });
        })
        .then(function (res) {
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.purged || 'Catalog purged.') : errorMessage(res.body, i18n.purgeFailed || 'Purge failed.'));
          load();
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.purgeFailed || 'Purge failed.'));
        });
    });
    load();
  }

  function initImageAdmin(root) {
    var t = token(root);
    var i18n = readI18n(root);
    var alert = root.querySelector('[data-ce-admin-alert]');
    var panel = root.querySelector('[data-ce-image-result]');
    var manifestStats = root.querySelector('[data-ce-image-manifest-stats]');
    var manifestRows = root.querySelector('[data-ce-image-manifest-rows]');
    root.querySelector('[data-ce-image-replace]')?.addEventListener('click', function () {
      var productId = parseInt(root.querySelector('[data-ce-image-product]')?.value || '0', 10);
      var sourceUrl = root.querySelector('[data-ce-image-url]')?.value || '';
      if (!productId || !sourceUrl.trim()) {
        showAlert(alert, 'error', i18n.needProductUrl || 'Enter a product id and source URL.');
        return;
      }
      showAlert(alert, 'info', i18n.replacing || 'Replacing image…');
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
          renderStatGrid(panel, [
            { label: i18n.statSuccess || 'Success', value: pick(res.body, 'success', 'Success') },
            { label: i18n.statPictureId || 'Picture id', value: pick(res.body, 'pictureId', 'PictureId'), code: true },
            { label: i18n.statPlaceholder || 'Placeholder', value: pick(res.body, 'usedPlaceholder', 'UsedPlaceholder') },
            { label: i18n.statQuarantined || 'Quarantined', value: pick(res.body, 'quarantined', 'Quarantined') },
            { label: i18n.statCdnUrl || 'CDN URL', value: pick(res.body, 'cdnUrl', 'CdnUrl') },
            { label: i18n.statError || 'Error', value: pick(res.body, 'errorCode', 'ErrorCode') || pick(res.body, 'reasonCode', 'ReasonCode') }
          ]);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.replaceComplete || 'Primary image replaced.') : errorMessage(res.body, i18n.replaceFailed || 'Replace failed.'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.replaceFailed || 'Replace failed.'));
        });
    });
    root.querySelector('[data-ce-image-manifest-submit]')?.addEventListener('click', function () {
      var csv = root.querySelector('[data-ce-image-manifest]')?.value || '';
      if (!csv.trim()) {
        showAlert(alert, 'error', i18n.needManifest || 'Paste a CSV manifest.');
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
          renderBatchImageResult(manifestStats, manifestRows, res.body, i18n);
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.manifestApplied || 'Manifest applied.') : errorMessage(res.body, i18n.manifestFailed || 'Manifest failed.'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.manifestFailed || 'Manifest failed.'));
        });
    });
    root.querySelector('[data-ce-image-template-submit]')?.addEventListener('click', function () {
      var raw = root.querySelector('[data-ce-image-template-skus]')?.value || '';
      var skus = raw.split(/[\s,;]+/).map(function (s) { return s.trim(); }).filter(Boolean);
      if (!skus.length) {
        showAlert(alert, 'error', i18n.needSkus || 'Enter one or more SKUs.');
        return;
      }
      showAlert(alert, 'info', i18n.applyingTemplate || 'Applying URL template…');
      fetch('/Admin/CheckEngine/ImageAdmin/SourceFromTemplate', {
        method: 'POST',
        headers: headers(t, true),
        credentials: 'same-origin',
        body: JSON.stringify({
          skus: skus,
          urlTemplate: root.querySelector('[data-ce-image-template-url]')?.value || ''
        })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          renderBatchImageResult(
            root.querySelector('[data-ce-image-template-stats]'),
            root.querySelector('[data-ce-image-template-rows]'),
            res.body,
            i18n
          );
          showAlert(alert, res.ok ? 'success' : 'error', res.ok ? (i18n.templateApplied || 'Template applied.') : errorMessage(res.body, i18n.templateFailed || 'Template failed.'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', errorMessage(err, i18n.templateFailed || 'Template failed.'));
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
