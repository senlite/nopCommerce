/* global window, document, fetch, JSON, navigator, alert */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function errorMessage(err, fallback) {
    var code = pick(err, 'reasonCode', 'ReasonCode') || pick(err, 'errorCode', 'ErrorCode') || '';
    if (code === 'licence.read_only')
      return 'Check Engine is in licence read-only mode. Activate a licence on the dashboard to make changes.';
    return code || fallback || 'Request failed.';
  }

  function headers(token, json) {
    var h = { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' };
    if (json) h['Content-Type'] = 'application/json';
    if (token && token.value) h.RequestVerificationToken = token.value;
    return h;
  }

  function statCard(label, value, hint) {
    return (
      '<article class="ce-mp-stat">' +
      '<span class="ce-mp-stat__label">' + label + '</span>' +
      '<span class="ce-mp-stat__value">' + (value != null ? value : '—') + '</span>' +
      (hint ? '<span class="ce-mp-stat__hint">' + hint + '</span>' : '') +
      '</article>'
    );
  }

  function pct(v) {
    return v == null ? '—' : (Math.round(v * 1000) / 10) + '%';
  }

  function showAlert(el, kind, message) {
    if (!el) return;
    el.hidden = !message;
    el.textContent = message || '';
    el.className = 'ce-mp-alert ce-mp-alert--' + (kind || 'info');
    el.setAttribute('role', kind === 'error' ? 'alert' : 'status');
  }

  function readI18n(root) {
    var node = root.querySelector('[data-ce-i18n]');
    if (!node) return {};
    try {
      return JSON.parse(node.textContent || '{}');
    } catch (e) {
      return {};
    }
  }

  function fmt(template) {
    var args = Array.prototype.slice.call(arguments, 1);
    return String(template == null ? '' : template).replace(/\{(\d+)\}/g, function (_, i) {
      return args[i] != null ? String(args[i]) : '';
    });
  }

  function setLoading(btn, loading, loadingText) {
    if (!btn) return;
    if (loading) {
      btn.setAttribute('aria-busy', 'true');
      btn.disabled = true;
      if (!btn.dataset.ceLabel) btn.dataset.ceLabel = btn.textContent;
      if (loadingText) btn.textContent = loadingText;
    } else {
      btn.removeAttribute('aria-busy');
      btn.disabled = false;
      if (btn.dataset.ceLabel) btn.textContent = btn.dataset.ceLabel;
    }
  }

  function validateEmail(value) {
    return value && value.indexOf('@') > 0;
  }

  function validateJsonArray(value) {
    if (!value || !value.trim()) return false;
    try {
      var parsed = JSON.parse(value);
      return Array.isArray(parsed) && parsed.length > 0;
    } catch (e) {
      return false;
    }
  }

  function initApply(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var form = root.querySelector('[data-ce-apply-form]');
    var success = root.querySelector('[data-ce-apply-success]');
    var alert = root.querySelector('[data-ce-apply-alert]');
    var submitBtn = root.querySelector('[data-ce-apply-submit]');

    if (!form || !submitBtn) return;

    function field(id) {
      return root.querySelector('#' + id);
    }

    function clearErrors() {
      form.querySelectorAll('[aria-invalid="true"]').forEach(function (el) {
        el.removeAttribute('aria-invalid');
      });
      form.querySelectorAll('.ce-mp-field__error').forEach(function (el) {
        el.textContent = '';
      });
    }

    function setError(inputId, message) {
      var input = field(inputId);
      if (!input) return;
      input.setAttribute('aria-invalid', 'true');
      var err = root.querySelector('[data-ce-error-for="' + inputId + '"]');
      if (err) err.textContent = message;
    }

    function validate() {
      clearErrors();
      var ok = true;
      if (!field('ce-vendor-legal').value.trim()) {
        setError('ce-vendor-legal', i18n.errorLegal || 'Required');
        ok = false;
      }
      if (!validateEmail(field('ce-vendor-email').value.trim())) {
        setError('ce-vendor-email', i18n.errorEmail || 'Invalid email');
        ok = false;
      }
      if (!validateJsonArray(field('ce-vendor-tax').value)) {
        setError('ce-vendor-tax', i18n.errorTax || 'Enter a JSON array');
        ok = false;
      }
      if (!field('ce-vendor-categories').value.trim()) {
        setError('ce-vendor-categories', i18n.errorCategories || 'Required');
        ok = false;
      }
      if (!field('ce-vendor-banking').value.trim()) {
        setError('ce-vendor-banking', i18n.errorBanking || 'Required');
        ok = false;
      }
      if (!field('ce-vendor-agree').checked) {
        showAlert(alert, 'error', i18n.errorAgreement || 'Accept the agreement');
        ok = false;
      }
      return ok;
    }

    submitBtn.addEventListener('click', function () {
      showAlert(alert, 'info', '');
      alert.hidden = true;
      if (!validate()) return;

      setLoading(submitBtn, true, i18n.submitting || 'Submitting…');

      fetch('/check-engine/vendor/apply', {
        method: 'POST',
        headers: headers(token, true),
        body: JSON.stringify({
          legalName: field('ce-vendor-legal').value.trim(),
          tradingName: field('ce-vendor-trading').value.trim(),
          contactEmail: field('ce-vendor-email').value.trim(),
          taxIdsJson: field('ce-vendor-tax').value.trim() || '[]',
          categoriesCsv: field('ce-vendor-categories').value.trim(),
          bankingDetails: field('ce-vendor-banking').value,
          submitForReview: true,
          acceptAgreement: field('ce-vendor-agree').checked
        })
      })
        .then(function (r) {
          return r.json().then(function (body) {
            return { ok: r.ok, body: body };
          });
        })
        .then(function (res) {
          setLoading(submitBtn, false);
          if (!res.ok) {
            showAlert(alert, 'error', errorMessage(res.body, i18n.errorSubmit || 'Submission failed'));
            return;
          }

          var vendor = pick(res.body, 'vendor', 'Vendor') || {};
          var vendorId = pick(vendor, 'id', 'Id') || pick(res.body, 'vendorId', 'VendorId');
          var accessToken = pick(res.body, 'applicantAccessToken', 'ApplicantAccessToken');

          form.hidden = true;
          success.hidden = false;

          root.querySelector('[data-ce-success-vendor-id]').textContent = vendorId || '—';
          root.querySelector('[data-ce-success-email]').textContent = pick(vendor, 'contactEmail', 'ContactEmail') || field('ce-vendor-email').value;

          var tokenBox = root.querySelector('[data-ce-success-token]');
          var tokenWrap = root.querySelector('[data-ce-token-wrap]');
          if (accessToken && tokenBox) {
            tokenBox.textContent = accessToken;
            if (tokenWrap) tokenWrap.hidden = false;
          } else if (tokenWrap) {
            tokenWrap.hidden = true;
          }

          var steps = root.querySelectorAll('.ce-mp-step');
          steps.forEach(function (step, idx) {
            step.classList.remove('ce-mp-step--active');
            if (idx < steps.length - 1) step.classList.add('ce-mp-step--done');
            else step.classList.add('ce-mp-step--active');
          });
        })
        .catch(function () {
          setLoading(submitBtn, false);
          showAlert(alert, 'error', i18n.errorNetwork || 'Network error');
        });
    });

    var copyBtn = root.querySelector('[data-ce-copy-token]');
    if (copyBtn) {
      copyBtn.addEventListener('click', function () {
        var value = root.querySelector('[data-ce-success-token]');
        if (!value || !value.textContent) return;
        navigator.clipboard.writeText(value.textContent).then(function () {
          showAlert(alert, 'success', i18n.copied || 'Copied');
        });
      });
    }
  }

  function initDashboard(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var tabs = root.querySelectorAll('[data-ce-tab]');
    var panels = root.querySelectorAll('[data-ce-tabpanel]');

    function activateTab(tabId) {
      if (!tabId) return;
      var tab = root.querySelector('[data-ce-tab="' + tabId + '"]');
      if (tab) tab.click();
    }

    tabs.forEach(function (tab) {
      tab.addEventListener('click', function () {
        var id = tab.getAttribute('data-ce-tab');
        tabs.forEach(function (t) {
          t.setAttribute('aria-selected', t === tab ? 'true' : 'false');
        });
        panels.forEach(function (p) {
          p.hidden = p.getAttribute('data-ce-tabpanel') !== id;
        });
      });
    });

    activateTab(new URLSearchParams(window.location.search).get('tab'));

    function renderStats(d) {
      var grid = root.querySelector('[data-ce-stat-grid]');
      if (!grid) return;
      var low = pick(d, 'lowStockCount', 'LowStockCount') || 0;
      grid.innerHTML =
        statCard(i18n.products, pick(d, 'productCount', 'ProductCount')) +
        statCard(i18n.stockUnits, pick(d, 'totalStockUnits', 'TotalStockUnits'), low + ' ' + (i18n.lowStock || 'low')) +
        statCard(i18n.orders, pick(d, 'orderCount', 'OrderCount')) +
        statCard(i18n.customers, pick(d, 'customerCount', 'CustomerCount'));
    }

    function renderScorecard(s) {
      var host = root.querySelector('[data-ce-scorecard]');
      if (!host || !s) return;
      host.innerHTML =
        metricBar(i18n.fillRate, pick(s, 'fillRate', 'FillRate'), true) +
        metricBar(i18n.cancelRate, pick(s, 'cancelRate', 'CancelRate'), false, true) +
        metricBar(i18n.claimReject, pick(s, 'claimRejectionRate', 'ClaimRejectionRate'), false, true) +
        metricBar(i18n.onTime, pick(s, 'onTimeShipmentRate', 'OnTimeShipmentRate'), true);
    }

    function metricBar(label, value, goodHigh, warn) {
      var pctVal = value == null ? 0 : Math.round(value * 100);
      var fillClass = warn ? 'ce-mp-metric__fill--warn' : goodHigh ? 'ce-mp-metric__fill--good' : 'ce-mp-metric__fill';
      return (
        '<div class="ce-mp-metric">' +
        '<div class="ce-mp-metric__head"><span>' + label + '</span><span>' + pct(value) + '</span></div>' +
        '<div class="ce-mp-metric__bar"><div class="ce-mp-metric__fill ' + fillClass + '" style="inline-size:' + pctVal + '%"></div></div>' +
        '</div>'
      );
    }

    function renderStatements(statements) {
      var el = root.querySelector('[data-ce-statements]');
      if (!el) return;
      if (pick(statements, 'available', 'Available')) {
        el.innerHTML =
          '<p><strong>#' + pick(statements, 'statementId', 'StatementId') + '</strong> — ' +
          (i18n.netPayout || 'Net') + ' ' + pick(statements, 'netPayout', 'NetPayout') +
          ' <span class="ce-code">(' + pick(statements, 'status', 'Status') + ')</span></p>';
      } else {
        el.innerHTML = '<p class="ce-mp-empty">' + (i18n.statementsPending || '') + '</p>';
      }
    }

    fetch('/check-engine/vendor/dashboarddata', { headers: headers(token, false) })
      .then(function (r) { return r.json(); })
      .then(function (d) {
        renderStats(d);
        renderScorecard(pick(d, 'scorecard', 'Scorecard'));
        renderStatements(pick(d, 'statements', 'Statements') || {});
      });

    fetch('/check-engine/vendor/inventory', { headers: headers(token, false) })
      .then(function (r) { return r.json(); })
      .then(function (items) {
        var tbody = root.querySelector('[data-ce-inventory-body]');
        if (!tbody) return;
        tbody.innerHTML = '';
        if (!items || !items.length) {
          tbody.innerHTML = '<tr><td colspan="3" class="ce-mp-empty">' + (i18n.emptyInventory || 'No products') + '</td></tr>';
          return;
        }
        items.forEach(function (item) {
          var productId = pick(item, 'productId', 'ProductId');
          var name = pick(item, 'name', 'Name') || '';
          var stock = pick(item, 'stockQuantity', 'StockQuantity');
          var tr = document.createElement('tr');
          tr.innerHTML =
            '<td><span class="ce-code">' + productId + '</span> — ' + name + '</td>' +
            '<td><input type="number" class="ce-mp-input ce-stock-input" data-product-id="' + productId + '" value="' + stock + '" min="0" /></td>' +
            '<td><button type="button" class="ce-btn ce-btn--quiet ce-save-stock" data-product-id="' + productId + '">' + (i18n.save || 'Save') + '</button></td>';
          tbody.appendChild(tr);
        });
      });

    root.addEventListener('click', function (e) {
      if (!e.target.classList.contains('ce-save-stock')) return;
      var pid = parseInt(e.target.getAttribute('data-product-id'), 10);
      var input = root.querySelector('.ce-stock-input[data-product-id="' + pid + '"]');
      fetch('/check-engine/vendor/updateinventory', {
        method: 'POST',
        headers: headers(token, true),
        body: JSON.stringify({ productId: pid, stockQuantity: parseInt(input.value, 10) || 0 })
      }).then(function (r) { return r.json(); }).then(function () {
        showAlert(root.querySelector('[data-ce-dashboard-alert]'), 'success', i18n.saved || 'Saved');
      });
    });

    function loadFitment() {
      fetch('/check-engine/vendor/fitmentproposals', { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(function (list) {
          var tbody = root.querySelector('[data-ce-fitment-body]');
          if (!tbody) return;
          tbody.innerHTML = '';
          if (!list || !list.length) {
            tbody.innerHTML = '<tr><td colspan="5" class="ce-mp-empty">' + (i18n.emptyFitment || 'No proposals') + '</td></tr>';
            return;
          }
          list.forEach(function (item) {
            var tr = document.createElement('tr');
            tr.innerHTML =
              '<td class="ce-code">' + pick(item, 'id', 'Id') + '</td>' +
              '<td class="ce-code">' + pick(item, 'productId', 'ProductId') + '</td>' +
              '<td class="ce-code">' + pick(item, 'vehicleConfigurationId', 'VehicleConfigurationId') + '</td>' +
              '<td>' + pick(item, 'status', 'Status') + '</td>' +
              '<td><button type="button" class="ce-btn ce-btn--quiet ce-revoke-fitment" data-claim-id="' + pick(item, 'id', 'Id') + '">' + (i18n.revoke || 'Revoke') + '</button></td>';
            tbody.appendChild(tr);
          });
        });
    }

    loadFitment();

    var fitSubmit = root.querySelector('[data-ce-fitment-submit]');
    if (fitSubmit) {
      fitSubmit.addEventListener('click', function () {
        fetch('/check-engine/vendor/submitfitmentproposal', {
          method: 'POST',
          headers: headers(token, true),
          body: JSON.stringify({
            productId: parseInt(root.querySelector('#ce-fitment-product').value, 10),
            vehicleConfigurationId: parseInt(root.querySelector('#ce-fitment-vehicle').value, 10)
          })
        }).then(function () { loadFitment(); });
      });
    }

    root.addEventListener('click', function (e) {
      if (!e.target.classList.contains('ce-revoke-fitment')) return;
      var claimId = parseInt(e.target.getAttribute('data-claim-id'), 10);
      fetch('/check-engine/vendor/revokefitmentproposal?claimId=' + claimId, {
        method: 'POST',
        headers: headers(token, false)
      }).then(function () { loadFitment(); });
    });

    function loadTable(url, bodySel, cols) {
      fetch(url, { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(function (rows) {
          var tbody = root.querySelector(bodySel);
          if (!tbody) return;
          tbody.innerHTML = '';
          if (!rows || !rows.length) {
            tbody.innerHTML = '<tr><td colspan="' + cols + '" class="ce-mp-empty">' + (i18n.empty || 'None') + '</td></tr>';
            return;
          }
          rows.forEach(function (row) {
            var tr = document.createElement('tr');
            tr.innerHTML = row;
            tbody.appendChild(tr);
          });
        });
    }

    fetch('/check-engine/vendor/orders', { headers: headers(token, false) })
      .then(function (r) { return r.json(); })
      .then(function (orders) {
        var tbody = root.querySelector('[data-ce-orders-body]');
        if (!tbody) return;
        tbody.innerHTML = '';
        if (!orders || !orders.length) {
          tbody.innerHTML = '<tr><td colspan="3" class="ce-mp-empty">' + (i18n.emptyOrders || 'No orders') + '</td></tr>';
          return;
        }
        orders.forEach(function (o) {
          var tr = document.createElement('tr');
          tr.innerHTML =
            '<td class="ce-code">' + pick(o, 'orderId', 'OrderId') + '</td>' +
            '<td class="ce-code">' + pick(o, 'customerId', 'CustomerId') + '</td>' +
            '<td>' + (pick(o, 'orderTotal', 'OrderTotal') != null ? pick(o, 'orderTotal', 'OrderTotal') : '—') + '</td>';
          tbody.appendChild(tr);
        });
      });

    fetch('/check-engine/vendor/catalog', { headers: headers(token, false) })
      .then(function (r) { return r.json(); })
      .then(function (ids) {
        var tbody = root.querySelector('[data-ce-catalog-body]');
        if (!tbody) return;
        tbody.innerHTML = '';
        if (!ids || !ids.length) {
          tbody.innerHTML = '<tr><td colspan="1" class="ce-mp-empty">' + (i18n.emptyCatalog || 'No catalog') + '</td></tr>';
          return;
        }
        ids.forEach(function (id) {
          var tr = document.createElement('tr');
          tr.innerHTML = '<td class="ce-code">' + id + '</td>';
          tbody.appendChild(tr);
        });
      });
  }

  function initCommission(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var vendorSelect = root.querySelector('[data-ce-commission-vendor]');
    var alert = root.querySelector('[data-ce-commission-alert]');
    var rulesHost = root.querySelector('[data-ce-rules]');
    var planName = root.querySelector('[data-ce-plan-name]');
    var planActive = root.querySelector('[data-ce-plan-active]');
    var planId = root.querySelector('[data-ce-plan-id]');
    var jsonArea = root.querySelector('[data-ce-commission-json]');
    var vendorIdInput = root.querySelector('[data-ce-vendor-id]');

    var modelKinds = [
      { v: 1, l: i18n.kindFlat || 'Flat fee' },
      { v: 2, l: i18n.kindPercentage || 'Percentage' },
      { v: 3, l: i18n.kindTiered || 'Tiered' },
      { v: 4, l: i18n.kindCategory || 'Category override' }
    ];
    var bases = [
      { v: 1, l: i18n.basisPerOrder || 'Per order' },
      { v: 2, l: i18n.basisPerLine || 'Per line item' },
      { v: 3, l: i18n.basisLineSubtotal || 'Line subtotal' },
      { v: 4, l: i18n.basisOrderSubtotal || 'Order subtotal' }
    ];

    function currentVendorId() {
      return parseInt(vendorSelect ? vendorSelect.value : vendorIdInput.value, 10) || 0;
    }

    function loadVendors() {
      if (!vendorSelect) return;
      fetch('/Admin/CheckEngine/VendorAdmin/Scorecards?json=1', { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(function (list) {
          vendorSelect.innerHTML = '';
          (list || []).forEach(function (row) {
            var id = pick(row, 'vendorId', 'VendorId');
            var name = pick(row, 'vendorName', 'VendorName') || ('Vendor ' + id);
            if (!id) return;
            var opt = document.createElement('option');
            opt.value = id;
            opt.textContent = id + ' — ' + name;
            vendorSelect.appendChild(opt);
          });
          if (vendorIdInput && vendorIdInput.value) vendorSelect.value = vendorIdInput.value;
          loadPlan();
        });
    }

    function planFromUi() {
      var rules = [];
      rulesHost.querySelectorAll('[data-ce-rule]').forEach(function (card, idx) {
        rules.push({
          id: parseInt(card.querySelector('[data-ce-rule-id]').value, 10) || 0,
          modelKind: parseInt(card.querySelector('[data-ce-rule-kind]').value, 10),
          basis: parseInt(card.querySelector('[data-ce-rule-basis]').value, 10),
          priority: parseInt(card.querySelector('[data-ce-rule-priority]').value, 10) || idx + 1,
          flatAmount: parseFloat(card.querySelector('[data-ce-rule-flat]').value) || null,
          percentageRate: parseFloat(card.querySelector('[data-ce-rule-percent]').value) || null,
          categoryId: parseInt(card.querySelector('[data-ce-rule-category]').value, 10) || null,
          isActive: card.querySelector('[data-ce-rule-active]').checked,
          tierBands: readTierBands(card)
        });
      });
      return {
        id: parseInt(planId.value, 10) || 0,
        vendorId: currentVendorId(),
        name: planName.value || 'Default',
        isActive: planActive.checked,
        rules: rules
      };
    }

    function readTierBands(card) {
      var bands = [];
      card.querySelectorAll('[data-ce-tier]').forEach(function (row) {
        bands.push({
          minVolume: parseFloat(row.querySelector('[data-ce-tier-min]').value) || 0,
          maxVolume: parseFloat(row.querySelector('[data-ce-tier-max]').value) || null,
          percentageRate: parseFloat(row.querySelector('[data-ce-tier-rate]').value) || 0
        });
      });
      return bands;
    }

    function renderRule(rule, idx) {
      var card = document.createElement('div');
      card.className = 'card card-default mb-3';
      card.setAttribute('data-ce-rule', '1');
      card.innerHTML =
        '<div class="card-header clearfix">' +
        '<h3 class="card-title float-left">' + fmt(i18n.rule || 'Rule {0}', idx + 1) + '</h3>' +
        '<button type="button" class="btn btn-sm btn-danger float-right" data-ce-remove-rule>' + (i18n.remove || 'Remove') + '</button>' +
        '</div><div class="card-body"><div class="form-row">' +
        '<input type="hidden" data-ce-rule-id value="' + (pick(rule, 'id', 'Id') || 0) + '" />' +
        selectField(i18n.model || 'Model', 'data-ce-rule-kind', modelKinds, pick(rule, 'modelKind', 'ModelKind') || 2) +
        selectField(i18n.basis || 'Basis', 'data-ce-rule-basis', bases, pick(rule, 'basis', 'Basis') || 3) +
        numberField(i18n.priority || 'Priority', 'data-ce-rule-priority', pick(rule, 'priority', 'Priority') || idx + 1) +
        numberField(i18n.flatAmount || 'Flat amount', 'data-ce-rule-flat', pick(rule, 'flatAmount', 'FlatAmount')) +
        numberField(i18n.percent || 'Percent %', 'data-ce-rule-percent', pick(rule, 'percentageRate', 'PercentageRate')) +
        numberField(i18n.categoryId || 'Category id', 'data-ce-rule-category', pick(rule, 'categoryId', 'CategoryId')) +
        '<div class="form-group col-md-3 d-flex align-items-end">' +
        '<label class="mb-0"><input type="checkbox" data-ce-rule-active ' + (pick(rule, 'isActive', 'IsActive') !== false ? 'checked' : '') + ' /> ' + (i18n.active || 'Active') + '</label></div>' +
        '</div><div data-ce-tiers></div>' +
        '<button type="button" class="btn btn-sm btn-secondary mt-2" data-ce-add-tier>' + (i18n.addTier || 'Add tier band') + '</button></div>';
      var tiersHost = card.querySelector('[data-ce-tiers]');
      (pick(rule, 'tierBands', 'TierBands') || []).forEach(function (band) {
        tiersHost.appendChild(tierRow(band));
      });
      card.querySelector('[data-ce-add-tier]').addEventListener('click', function () {
        tiersHost.appendChild(tierRow({}));
      });
      card.querySelector('[data-ce-remove-rule]').addEventListener('click', function () {
        card.remove();
        syncJson();
      });
      card.querySelectorAll('input,select').forEach(function (el) {
        el.addEventListener('change', syncJson);
      });
      return card;
    }

    function selectField(label, attr, options, selected) {
      var html = '<div class="form-group col-md-3"><label>' + label + '</label><select class="form-control" ' + attr + '>';
      options.forEach(function (o) {
        html += '<option value="' + o.v + '"' + (selected === o.v ? ' selected' : '') + '>' + o.l + '</option>';
      });
      html += '</select></div>';
      return html;
    }

    function numberField(label, attr, value) {
      return '<div class="form-group col-md-3"><label>' + label + '</label>' +
        '<input type="number" step="any" class="form-control" ' + attr + ' value="' + (value != null ? value : '') + '" /></div>';
    }

    function tierRow(band) {
      var row = document.createElement('div');
      row.className = 'form-row mt-2';
      row.setAttribute('data-ce-tier', '1');
      row.innerHTML =
        numberField(i18n.minVolume || 'Min volume', 'data-ce-tier-min', pick(band, 'minVolume', 'MinVolume')) +
        numberField(i18n.maxVolume || 'Max volume', 'data-ce-tier-max', pick(band, 'maxVolume', 'MaxVolume')) +
        numberField(i18n.ratePercent || 'Rate %', 'data-ce-tier-rate', pick(band, 'percentageRate', 'PercentageRate')) +
        '<div class="form-group col-md-3 d-flex align-items-end">' +
        '<button type="button" class="btn btn-sm btn-outline-danger" data-ce-remove-tier>' + (i18n.removeTier || 'Remove tier') + '</button></div>';
      row.querySelector('[data-ce-remove-tier]').addEventListener('click', function () {
        row.remove();
        syncJson();
      });
      row.querySelectorAll('input').forEach(function (el) {
        el.addEventListener('change', syncJson);
      });
      return row;
    }

    function renderPlan(plan) {
      planId.value = pick(plan, 'id', 'Id') || 0;
      planName.value = pick(plan, 'name', 'Name') || 'Default';
      planActive.checked = pick(plan, 'isActive', 'IsActive') !== false;
      rulesHost.innerHTML = '';
      var rules = pick(plan, 'rules', 'Rules') || [];
      if (!rules.length) rules = [{ modelKind: 2, basis: 3, priority: 1, isActive: true }];
      rules.forEach(function (rule, idx) {
        rulesHost.appendChild(renderRule(rule, idx));
      });
      syncJson();
    }

    function syncJson() {
      if (jsonArea) jsonArea.value = JSON.stringify(planFromUi(), null, 2);
    }

    function loadPlan() {
      var vid = currentVendorId();
      if (!vid) return;
      fetch('/Admin/CheckEngine/CommissionAdmin/GetPlan?vendorId=' + vid, { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(renderPlan);
    }

    if (vendorSelect) {
      vendorSelect.addEventListener('change', loadPlan);
      loadVendors();
    } else {
      loadPlan();
    }

    root.querySelector('[data-ce-add-rule]').addEventListener('click', function () {
      rulesHost.appendChild(renderRule({ modelKind: 2, basis: 3, priority: rulesHost.children.length + 1, isActive: true }, rulesHost.children.length));
      syncJson();
    });

    root.querySelector('[data-ce-save-plan]').addEventListener('click', function () {
      var body = planFromUi();
      fetch('/Admin/CheckEngine/CommissionAdmin/SavePlan', {
        method: 'POST',
        headers: headers(token, true),
        body: JSON.stringify(body)
      })
        .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, body: j }; }); })
        .then(function (res) {
          if (res.ok) {
            renderPlan(res.body);
            showAlert(alert, 'success', i18n.planSaved || 'Commission plan saved.');
          } else {
            showAlert(alert, 'error', errorMessage(res.body, i18n.saveFailed || 'Save failed'));
          }
        });
    });

    if (jsonArea) {
      root.querySelector('[data-ce-import-json]').addEventListener('click', function () {
        try {
          renderPlan(JSON.parse(jsonArea.value));
        } catch (e) {
          showAlert(alert, 'error', i18n.invalidJson || 'Invalid JSON');
        }
      });
    }
  }

  function statusBadge(status) {
    var s = (status || '').toString();
    var cls = 'ce-mp-badge--muted';
    if (/active|reconciled|finalized|pushed/i.test(s)) cls = 'ce-mp-badge--good';
    else if (/review|underreview|draft|applied/i.test(s)) cls = 'ce-mp-badge--info';
    else if (/reject|suspend|closed/i.test(s)) cls = 'ce-mp-badge--warn';
    return '<span class="ce-mp-badge ' + cls + '">' + s + '</span>';
  }

  function formatDate(iso) {
    if (!iso) return '—';
    try {
      return new Date(iso).toLocaleDateString();
    } catch (e) {
      return iso;
    }
  }

  function formatMoney(v) {
    return v == null ? '—' : Number(v).toFixed(2);
  }

  function initVendorReview(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-review-alert]');
    var tbody = root.querySelector('[data-ce-review-body]');

    function loadQueue() {
      if (tbody) tbody.innerHTML = '<tr><td colspan="8" class="ce-mp-empty ce-mp-loading">' + (i18n.loading || 'Loading…') + '</td></tr>';
      fetch(root.getAttribute('data-ce-queue-url') || '/Admin/CheckEngine/VendorAdmin/Queue', { headers: headers(token, false) })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (!tbody) return;
          tbody.innerHTML = '';
          if (!res.ok) {
            showAlert(alert, 'error', errorMessage(res.body, i18n.errorLoad || 'Could not load queue'));
            return;
          }
          var rows = res.body || [];
          if (!rows.length) {
            tbody.innerHTML = '<tr><td colspan="8" class="ce-mp-empty">' + (i18n.empty || 'No applications in queue') + '</td></tr>';
            return;
          }
          rows.forEach(function (row) {
            var vendor = pick(row, 'vendor', 'Vendor') || {};
            var id = pick(vendor, 'id', 'Id');
            var tr = document.createElement('tr');
            tr.innerHTML =
              '<td class="ce-code">' + id + '</td>' +
              '<td><strong>' + (pick(vendor, 'legalName', 'LegalName') || '') + '</strong></td>' +
              '<td>' + (pick(vendor, 'contactEmail', 'ContactEmail') || '') + '</td>' +
              '<td>' + (pick(vendor, 'categoriesCsv', 'CategoriesCsv') || '') + '</td>' +
              '<td>' + statusBadge(pick(vendor, 'status', 'Status')) + '</td>' +
              '<td>' + (pick(row, 'acceptedAgreementVersion', 'AcceptedAgreementVersion') || '—') + '</td>' +
              '<td>' + ((pick(vendor, 'hasBankingDetails', 'HasBankingDetails')) ? (i18n.yes || 'Yes') : (i18n.no || 'No')) + '</td>' +
              '<td class="ce-mp-actions">' +
              '<button type="button" class="btn btn-sm btn-secondary" data-act="Submit">' + (i18n.review || 'Review') + '</button> ' +
              '<button type="button" class="btn btn-sm btn-success" data-act="Approve">' + (i18n.approve || 'Approve') + '</button> ' +
              '<button type="button" class="btn btn-sm btn-danger" data-act="Reject">' + (i18n.reject || 'Reject') + '</button>' +
              '</td>';
            tr.querySelectorAll('button').forEach(function (btn) {
              btn.addEventListener('click', function () {
                act(btn.getAttribute('data-act'), id);
              });
            });
            tbody.appendChild(tr);
          });
        });
    }

    function act(action, vendorId) {
      var notes = null;
      if (action === 'Reject') {
        notes = window.prompt(i18n.rejectNotes || 'Rejection notes (optional):', '');
        if (notes === null) return;
      }
      fetch('/Admin/CheckEngine/VendorAdmin/' + action, {
        method: 'POST',
        headers: headers(token, true),
        body: JSON.stringify({ vendorId: vendorId, notes: notes })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (res.ok) {
            showAlert(alert, 'success', (i18n.actionOk || 'Updated') + ' #' + vendorId);
            loadQueue();
          } else {
            showAlert(alert, 'error', errorMessage(res.body, i18n.actionFail || 'Action failed'));
          }
        });
    }

    var refresh = root.querySelector('[data-ce-review-refresh]');
    if (refresh) refresh.addEventListener('click', loadQueue);
    loadQueue();
  }

  function initScoreboard(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var tbody = root.querySelector('[data-ce-scoreboard-body]');
    var summary = root.querySelector('[data-ce-scoreboard-summary]');

    fetch('/Admin/CheckEngine/VendorAdmin/Scorecards?json=1', { headers: headers(token, false) })
      .then(function (r) { return r.json(); })
      .then(function (rows) {
        if (summary) {
          summary.innerHTML =
            statCard(i18n.vendors || 'Active vendors', (rows || []).length) +
            statCard(i18n.ordersSample || 'Orders sampled', sumField(rows, 'orderSampleSize', 'OrderSampleSize'));
        }
        if (!tbody) return;
        tbody.innerHTML = '';
        if (!rows || !rows.length) {
          tbody.innerHTML = '<tr><td colspan="6" class="ce-mp-empty">' + (i18n.empty || 'No scorecard data yet') + '</td></tr>';
          return;
        }
        rows.forEach(function (row) {
          var tr = document.createElement('tr');
          var name = pick(row, 'vendorName', 'VendorName') || pick(row, 'vendorId', 'VendorId');
          tr.innerHTML =
            '<td><strong>' + name + '</strong><div class="ce-code">#' + (pick(row, 'vendorId', 'VendorId') || '') + '</div></td>' +
            '<td>' + metricCell(pick(row, 'fillRate', 'FillRate'), true) + '</td>' +
            '<td>' + metricCell(pick(row, 'cancelRate', 'CancelRate'), false, true) + '</td>' +
            '<td>' + metricCell(pick(row, 'claimRejectionRate', 'ClaimRejectionRate'), false, true) + '</td>' +
            '<td>' + metricCell(pick(row, 'onTimeShipmentRate', 'OnTimeShipmentRate'), true) + '</td>' +
            '<td>' + (pick(row, 'orderSampleSize', 'OrderSampleSize') || 0) + '</td>';
          tbody.appendChild(tr);
        });
      });

    function sumField(rows, camel, pascal) {
      return (rows || []).reduce(function (acc, row) {
        return acc + (pick(row, camel, pascal) || 0);
      }, 0);
    }

    function metricCell(value, goodHigh, warn) {
      var pctVal = value == null ? 0 : Math.round(value * 100);
      var fillClass = warn ? 'ce-mp-metric__fill--warn' : goodHigh ? 'ce-mp-metric__fill--good' : 'ce-mp-metric__fill';
      return (
        '<div class="ce-mp-metric">' +
        '<div class="ce-mp-metric__head"><span>' + pct(value) + '</span></div>' +
        '<div class="ce-mp-metric__bar"><div class="ce-mp-metric__fill ' + fillClass + '" style="inline-size:' + pctVal + '%"></div></div>' +
        '</div>'
      );
    }

    function cell(val) {
      return val == null || val === '' ? '—' : String(val);
    }

    function fillTable(tbody, rows, cols, emptyText) {
      if (!tbody) return;
      if (!rows || !rows.length) {
        tbody.innerHTML = '<tr><td colspan="' + cols + '">' + (emptyText || i18n.noRows || 'No rows.') + '</td></tr>';
        return;
      }
      tbody.innerHTML = rows.join('');
    }

    function inspectOrder() {
      var orderAlert = root.querySelector('[data-ce-order-alert]');
      var orderId = Number(root.querySelector('[data-ce-order-id]')?.value || 0);
      var stats = root.querySelector('[data-ce-order-stats]');
      if (!orderId) {
        showAlert(orderAlert, 'error', i18n.inspectEmpty || 'Enter an order id.');
        return;
      }
      showAlert(orderAlert, 'info', i18n.loading || 'Loading…');
      Promise.all([
        fetch('/Admin/CheckEngine/VendorAdmin/OrderSplits?orderId=' + orderId + '&json=1', { headers: headers(token, false) })
          .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }).catch(function () { return { ok: r.ok, body: {} }; }); }),
        fetch('/Admin/CheckEngine/CommissionAdmin/OrderSnapshots?orderId=' + orderId + '&json=1', { headers: headers(token, false) })
          .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }).catch(function () { return { ok: r.ok, body: [] }; }); })
      ]).then(function (results) {
        var splitsRes = results[0];
        var snapsRes = results[1];
        var checkout = pick(splitsRes.body, 'checkout', 'Checkout') || splitsRes.body || {};
        var splits = pick(checkout, 'splits', 'Splits') || [];
        var shipments = pick(splitsRes.body, 'shipments', 'Shipments') || [];
        var snaps = Array.isArray(snapsRes.body) ? snapsRes.body : (pick(snapsRes.body, 'items', 'Items') || []);
        if (stats) {
          stats.innerHTML =
            statCard(i18n.statOrder || 'Order', orderId) +
            statCard(i18n.statCheckoutGroup || 'Checkout group', pick(checkout, 'checkoutGroupId', 'CheckoutGroupId') || '—') +
            statCard(i18n.statSplits || 'Splits', splits.length) +
            statCard(i18n.statShipments || 'Shipments', shipments.length) +
            statCard(i18n.statSnapshots || 'Snapshots', snaps.length);
        }
        fillTable(root.querySelector('[data-ce-order-splits]'), splits.map(function (row) {
          var lines = pick(row, 'lines', 'Lines') || [];
          return '<tr><td class="ce-code">' + cell(pick(row, 'vendorId', 'VendorId')) + '</td><td>' +
            cell(pick(row, 'lineSubtotalExclTax', 'LineSubtotalExclTax')) + '</td><td>' + lines.length + '</td></tr>';
        }), 3);
        fillTable(root.querySelector('[data-ce-order-shipments]'), shipments.map(function (row) {
          return '<tr><td class="ce-code">' + cell(pick(row, 'shipmentId', 'ShipmentId')) + '</td><td class="ce-code">' +
            cell(pick(row, 'vendorId', 'VendorId')) + '</td><td>' + cell(pick(row, 'createdUtc', 'CreatedUtc')) + '</td></tr>';
        }), 3);
        fillTable(root.querySelector('[data-ce-order-commissions]'), snaps.map(function (row) {
          return '<tr><td class="ce-code">' + cell(pick(row, 'orderItemId', 'OrderItemId')) + '</td><td class="ce-code">' +
            cell(pick(row, 'vendorId', 'VendorId')) + '</td><td>' + cell(pick(row, 'modelKind', 'ModelKind')) +
            '</td><td>' + cell(pick(row, 'rateApplied', 'RateApplied')) + '</td><td>' +
            cell(pick(row, 'commissionAmount', 'CommissionAmount')) + '</td></tr>';
        }), 5);
        if (!splitsRes.ok && !snaps.length) {
          showAlert(orderAlert, 'error', i18n.inspectFail || 'Order not found.');
        } else {
          showAlert(orderAlert, 'success', fmt(i18n.loaded || 'Order #{0} loaded.', orderId));
        }
      }).catch(function () {
        showAlert(orderAlert, 'error', i18n.inspectFail || 'Order not found.');
      });
    }

    root.querySelector('[data-ce-order-inspect]')?.addEventListener('click', inspectOrder);
    var initialOrder = root.querySelector('[data-ce-order-id]')?.value;
    if (initialOrder && Number(initialOrder) > 0) inspectOrder();
  }

  function initPayout(root) {
    var i18n = readI18n(root);
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-payout-alert]');
    var vendorSelect = root.querySelector('[data-ce-payout-vendor]');
    var tbody = root.querySelector('[data-ce-payout-body]');
    var detail = root.querySelector('[data-ce-payout-detail]');
    var selectedId = 0;

    function loadVendors() {
      if (!vendorSelect) return;
      fetch('/Admin/CheckEngine/VendorAdmin/Scorecards?json=1', { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(function (list) {
          vendorSelect.innerHTML = '';
          (list || []).forEach(function (row) {
            var id = pick(row, 'vendorId', 'VendorId');
            if (!id) return;
            var opt = document.createElement('option');
            opt.value = id;
            opt.textContent = id + ' — ' + (pick(row, 'vendorName', 'VendorName') || ('Vendor ' + id));
            vendorSelect.appendChild(opt);
          });
          loadStatements();
        });
    }

    function currentVendorId() {
      return parseInt(vendorSelect ? vendorSelect.value : '0', 10) || 0;
    }

    function loadStatements() {
      var vendorId = currentVendorId();
      if (!vendorId || !tbody) return;
      tbody.innerHTML = '<tr><td colspan="7" class="ce-mp-empty ce-mp-loading">' + (i18n.loading || 'Loading…') + '</td></tr>';
      fetch('/Admin/CheckEngine/PayoutAdmin/List?vendorId=' + vendorId, { headers: headers(token, false) })
        .then(function (r) { return r.json(); })
        .then(function (rows) {
          tbody.innerHTML = '';
          if (!rows || !rows.length) {
            tbody.innerHTML = '<tr><td colspan="7" class="ce-mp-empty">' + (i18n.empty || 'No statements') + '</td></tr>';
            if (detail) detail.hidden = true;
            return;
          }
          rows.forEach(function (stmt) {
            var id = pick(stmt, 'id', 'Id');
            var tr = document.createElement('tr');
            tr.innerHTML =
              '<td class="ce-code">' + id + '</td>' +
              '<td>' + formatDate(pick(stmt, 'periodStartUtc', 'PeriodStartUtc')) + ' – ' + formatDate(pick(stmt, 'periodEndUtc', 'PeriodEndUtc')) + '</td>' +
              '<td>' + formatMoney(pick(stmt, 'grossSales', 'GrossSales')) + '</td>' +
              '<td>' + formatMoney(pick(stmt, 'totalCommission', 'TotalCommission')) + '</td>' +
              '<td>' + formatMoney(pick(stmt, 'netPayout', 'NetPayout')) + '</td>' +
              '<td>' + statusBadge(pick(stmt, 'status', 'Status')) + '</td>' +
              '<td><button type="button" class="btn btn-sm btn-secondary" data-statement-id="' + id + '">' + (i18n.view || 'View') + '</button></td>';
            tr.querySelector('button').addEventListener('click', function () {
              selectedId = id;
              showDetail(stmt);
              tbody.querySelectorAll('tr').forEach(function (r) { r.classList.remove('ce-mp-row--selected'); });
              tr.classList.add('ce-mp-row--selected');
            });
            tbody.appendChild(tr);
          });
        });
    }

    function showDetail(stmt) {
      if (!detail) return;
      detail.hidden = false;
      detail.querySelector('[data-ce-payout-statement-id]').textContent = '#' + pick(stmt, 'id', 'Id');
      detail.querySelector('[data-ce-payout-gross]').textContent = formatMoney(pick(stmt, 'grossSales', 'GrossSales'));
      detail.querySelector('[data-ce-payout-commission]').textContent = formatMoney(pick(stmt, 'totalCommission', 'TotalCommission'));
      detail.querySelector('[data-ce-payout-refunds]').textContent = formatMoney(pick(stmt, 'totalRefunds', 'TotalRefunds'));
      detail.querySelector('[data-ce-payout-adjustments]').textContent = formatMoney(pick(stmt, 'totalAdjustments', 'TotalAdjustments'));
      detail.querySelector('[data-ce-payout-net]').textContent = formatMoney(pick(stmt, 'netPayout', 'NetPayout'));
      detail.querySelector('[data-ce-payout-erp]').textContent = pick(stmt, 'erpReferenceId', 'ErpReferenceId') || '—';
      detail.querySelector('[data-ce-payout-status]').innerHTML = statusBadge(pick(stmt, 'status', 'Status'));
      var recon = detail.querySelector('[data-ce-payout-recon]');
      if (recon) recon.hidden = true;
    }

    function postAction(path, onSuccess) {
      if (!selectedId) {
        showAlert(alert, 'error', i18n.selectStatement || 'Select a statement first');
        return;
      }
      fetch('/Admin/CheckEngine/PayoutAdmin/' + path, {
        method: 'POST',
        headers: headers(token, true),
        body: JSON.stringify({ statementId: selectedId })
      })
        .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
        .then(function (res) {
          if (res.ok) {
            showAlert(alert, 'success', i18n.actionOk || 'Updated');
            if (onSuccess) onSuccess(res.body);
            loadStatements();
          } else {
            showAlert(alert, 'error', errorMessage(res.body, i18n.actionFail || 'Action failed'));
          }
        });
    }

    var generateBtn = root.querySelector('[data-ce-payout-generate]');
    if (generateBtn) {
      generateBtn.addEventListener('click', function () {
        var vendorId = currentVendorId();
        var startInput = root.querySelector('[data-ce-payout-start]');
        var endInput = root.querySelector('[data-ce-payout-end]');
        var end = endInput && endInput.value ? new Date(endInput.value) : new Date();
        var start = startInput && startInput.value ? new Date(startInput.value) : new Date(end.getTime() - 30 * 86400000);
        setLoading(generateBtn, true, i18n.generating || 'Generating…');
        fetch('/Admin/CheckEngine/PayoutAdmin/Generate', {
          method: 'POST',
          headers: headers(token, true),
          body: JSON.stringify({ vendorId: vendorId, periodStartUtc: start.toISOString(), periodEndUtc: end.toISOString() })
        })
          .then(function (r) { return r.json().then(function (body) { return { ok: r.ok, body: body }; }); })
          .then(function (res) {
            setLoading(generateBtn, false);
            if (res.ok) {
              showAlert(alert, 'success', (i18n.generated || 'Statement created') + ' #' + pick(res.body, 'id', 'Id'));
              loadStatements();
            } else {
              showAlert(alert, 'error', errorMessage(res.body, i18n.actionFail || 'Generate failed'));
            }
          });
      });
    }

    var finalizeBtn = root.querySelector('[data-ce-payout-finalize]');
    if (finalizeBtn) finalizeBtn.addEventListener('click', function () {
      postAction('Finalize', showDetail);
    });
    var pushBtn = root.querySelector('[data-ce-payout-push]');
    if (pushBtn) pushBtn.addEventListener('click', function () {
      postAction('PushErp', showDetail);
    });
    var reconcileBtn = root.querySelector('[data-ce-payout-reconcile]');
    if (reconcileBtn) reconcileBtn.addEventListener('click', function () {
      postAction('Reconcile', function (body) {
        var recon = root.querySelector('[data-ce-payout-recon]');
        if (!recon) return;
        recon.hidden = false;
        function yn(value) {
          if (value === true || value === 'true') return 'Yes';
          if (value === false || value === 'false') return 'No';
          return value == null || value === '' ? '—' : String(value);
        }
        function set(sel, val) {
          var el = recon.querySelector(sel);
          if (el) el.textContent = val == null || val === '' ? '—' : String(val);
        }
        set('[data-ce-payout-recon-succeeded]', yn(pick(body, 'succeeded', 'Succeeded')));
        set('[data-ce-payout-recon-local]', pick(body, 'localNetPayout', 'LocalNetPayout'));
        set('[data-ce-payout-recon-erp]', pick(body, 'erpTotal', 'ErpTotal'));
        set('[data-ce-payout-recon-variance]', pick(body, 'variance', 'Variance'));
        set('[data-ce-payout-recon-ok]', yn(pick(body, 'withinTolerance', 'WithinTolerance')));
      });
    });

    if (vendorSelect) vendorSelect.addEventListener('change', loadStatements);
    var refresh = root.querySelector('[data-ce-payout-refresh]');
    if (refresh) refresh.addEventListener('click', loadStatements);

    var endDefault = root.querySelector('[data-ce-payout-end]');
    var startDefault = root.querySelector('[data-ce-payout-start]');
    if (endDefault && !endDefault.value) {
      var now = new Date();
      endDefault.value = now.toISOString().slice(0, 10);
      if (startDefault) {
        var start = new Date(now.getTime() - 30 * 86400000);
        startDefault.value = start.toISOString().slice(0, 10);
      }
    }

    loadVendors();
  }

  function init() {
    document.querySelectorAll('[data-ce-page="apply"]').forEach(initApply);
    document.querySelectorAll('[data-ce-page="dashboard"]').forEach(initDashboard);
    document.querySelectorAll('[data-ce-page="commission"]').forEach(initCommission);
    document.querySelectorAll('[data-ce-page="vendor-review"]').forEach(initVendorReview);
    document.querySelectorAll('[data-ce-page="scoreboard"]').forEach(initScoreboard);
    document.querySelectorAll('[data-ce-page="payout"]').forEach(initPayout);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.CeMarketplace = { pick: pick, headers: headers, pct: pct };
})(window);
