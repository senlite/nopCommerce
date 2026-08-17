/* global window, document, fetch, JSON, navigator, alert */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function headers(token, json) {
    var h = { Accept: 'application/json' };
    if (json) h['Content-Type'] = 'application/json';
    if (token && token.value) h.RequestVerificationToken = token.value;
    return h;
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
            showAlert(alert, 'error', pick(res.body, 'reasonCode', 'ReasonCode') || i18n.errorSubmit || 'Submission failed');
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

    function statCard(label, value, hint) {
      return (
        '<article class="ce-mp-stat">' +
        '<span class="ce-mp-stat__label">' + label + '</span>' +
        '<span class="ce-mp-stat__value">' + (value != null ? value : '—') + '</span>' +
        (hint ? '<span class="ce-mp-stat__hint">' + hint + '</span>' : '') +
        '</article>'
      );
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
      { v: 1, l: 'Flat fee' },
      { v: 2, l: 'Percentage' },
      { v: 3, l: 'Tiered' },
      { v: 4, l: 'Category override' }
    ];
    var bases = [
      { v: 1, l: 'Per order' },
      { v: 2, l: 'Per line item' },
      { v: 3, l: 'Line subtotal' },
      { v: 4, l: 'Order subtotal' }
    ];

    function currentVendorId() {
      return parseInt(vendorSelect ? vendorSelect.value : vendorIdInput.value, 10) || 0;
    }

    function loadVendors() {
      if (!vendorSelect) return;
      fetch('/Admin/CheckEngine/VendorAdmin/Scorecards', { headers: headers(token, false) })
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
        '<h3 class="card-title float-left">Rule ' + (idx + 1) + '</h3>' +
        '<button type="button" class="btn btn-sm btn-danger float-right" data-ce-remove-rule>Remove</button>' +
        '</div><div class="card-body"><div class="form-row">' +
        '<input type="hidden" data-ce-rule-id value="' + (pick(rule, 'id', 'Id') || 0) + '" />' +
        selectField('Model', 'data-ce-rule-kind', modelKinds, pick(rule, 'modelKind', 'ModelKind') || 2) +
        selectField('Basis', 'data-ce-rule-basis', bases, pick(rule, 'basis', 'Basis') || 3) +
        numberField('Priority', 'data-ce-rule-priority', pick(rule, 'priority', 'Priority') || idx + 1) +
        numberField('Flat amount', 'data-ce-rule-flat', pick(rule, 'flatAmount', 'FlatAmount')) +
        numberField('Percent %', 'data-ce-rule-percent', pick(rule, 'percentageRate', 'PercentageRate')) +
        numberField('Category id', 'data-ce-rule-category', pick(rule, 'categoryId', 'CategoryId')) +
        '<div class="form-group col-md-3 d-flex align-items-end">' +
        '<label class="mb-0"><input type="checkbox" data-ce-rule-active ' + (pick(rule, 'isActive', 'IsActive') !== false ? 'checked' : '') + ' /> Active</label></div>' +
        '</div><div data-ce-tiers></div>' +
        '<button type="button" class="btn btn-sm btn-secondary mt-2" data-ce-add-tier>Add tier band</button></div>';
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
        numberField('Min volume', 'data-ce-tier-min', pick(band, 'minVolume', 'MinVolume')) +
        numberField('Max volume', 'data-ce-tier-max', pick(band, 'maxVolume', 'MaxVolume')) +
        numberField('Rate %', 'data-ce-tier-rate', pick(band, 'percentageRate', 'PercentageRate')) +
        '<div class="form-group col-md-3 d-flex align-items-end">' +
        '<button type="button" class="btn btn-sm btn-outline-danger" data-ce-remove-tier>Remove tier</button></div>';
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
            showAlert(alert, 'success', 'Commission plan saved.');
          } else {
            showAlert(alert, 'error', pick(res.body, 'reasonCode', 'ReasonCode') || 'Save failed');
          }
        });
    });

    if (jsonArea) {
      root.querySelector('[data-ce-import-json]').addEventListener('click', function () {
        try {
          renderPlan(JSON.parse(jsonArea.value));
        } catch (e) {
          showAlert(alert, 'error', 'Invalid JSON');
        }
      });
    }
  }

  function init() {
    document.querySelectorAll('[data-ce-page="apply"]').forEach(initApply);
    document.querySelectorAll('[data-ce-page="dashboard"]').forEach(initDashboard);
    document.querySelectorAll('[data-ce-page="commission"]').forEach(initCommission);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.CeMarketplace = { pick: pick, headers: headers, pct: pct };
})(window);
