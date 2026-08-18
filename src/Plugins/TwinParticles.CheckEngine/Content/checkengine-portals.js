/* global window, document, fetch */
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

  function showAlert(el, kind, message) {
    if (!el) return;
    el.hidden = !message;
    el.textContent = message || '';
    el.className = 'ce-mp-alert ce-mp-alert--' + (kind || 'info');
  }

  function apiGet(url, token) {
    return fetch(url, { headers: headers(token), credentials: 'same-origin' }).then(function (r) {
      return r.json().then(function (body) {
        if (!r.ok) throw body;
        return body;
      });
    });
  }

  function apiPost(url, token, payload) {
    return fetch(url, {
      method: 'POST',
      headers: headers(token, true),
      credentials: 'same-origin',
      body: JSON.stringify(payload || {})
    }).then(function (r) {
      return r.json().then(function (body) {
        if (!r.ok) throw body;
        return body;
      });
    });
  }

  function statusBadge(status) {
    var label = status;
    if (typeof status === 'number') {
      var map = { 0: 'Draft', 10: 'In progress', 20: 'Awaiting parts', 30: 'Ready', 40: 'Invoiced', 90: 'Cancelled' };
      label = map[status] || String(status);
    }
    return '<span class="ce-mp-badge">' + label + '</span>';
  }

  function initWorkshop(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var stats = root.querySelector('[data-ce-workshop-stats]');
    var jobsBody = root.querySelector('[data-ce-workshop-jobs]');
    var state = { accountId: null, jobs: [] };

    function render() {
      if (!stats) return;
      var account = state.account || {};
      var open = state.jobs.filter(function (j) {
        var s = pick(j, 'status', 'Status');
        return s !== 40 && s !== 90;
      }).length;
      stats.innerHTML =
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Account</span><span class="ce-mp-stat__value">' +
        (pick(account, 'displayName', 'DisplayName') || '—') +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Open jobs</span><span class="ce-mp-stat__value">' +
        open +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Credit used</span><span class="ce-mp-stat__value">' +
        (pick(account, 'creditUsed', 'CreditUsed') || 0) +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Credit limit</span><span class="ce-mp-stat__value">' +
        (pick(account, 'creditLimit', 'CreditLimit') || 0) +
        '</span></div>';
    }

    function renderJobs() {
      if (!jobsBody) return;
      if (!state.jobs.length) {
        jobsBody.innerHTML = '<tr><td colspan="5">No jobs yet.</td></tr>';
        return;
      }
      jobsBody.innerHTML = state.jobs
        .map(function (job) {
          var id = pick(job, 'id', 'Id');
          var status = pick(job, 'status', 'Status');
          var statusId = typeof status === 'number' ? status : pick(job, 'statusId', 'StatusId');
          return (
            '<tr data-job-id="' +
            id +
            '"><td>#' +
            id +
            '</td><td>' +
            statusBadge(statusId) +
            '</td><td>' +
            (pick(job, 'labourEstimate', 'LabourEstimate') || 0) +
            '</td><td>' +
            (pick(job, 'orderId', 'OrderId') || '—') +
            '</td><td><button type="button" class="ce-btn ce-btn--ghost" data-ce-workshop-ready="' +
            id +
            '">Mark ready</button> <button type="button" class="ce-btn ce-btn--primary" data-ce-workshop-invoice="' +
            id +
            '">Invoice</button></td></tr>'
          );
        })
        .join('');
    }

    function refresh() {
      return apiGet('/check-engine/workshop/DashboardData', token)
        .then(function (data) {
          state.account = pick(data, 'account', 'Account');
          state.accountId = pick(state.account, 'id', 'Id');
          state.jobs = pick(data, 'jobs', 'Jobs') || [];
          render();
          renderJobs();
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Failed to load workshop dashboard.');
        });
    }

    root.addEventListener('click', function (e) {
      var ready = e.target.closest('[data-ce-workshop-ready]');
      if (ready) {
        apiPost('/check-engine/workshop/TransitionJobStatus', token, {
          jobId: Number(ready.getAttribute('data-ce-workshop-ready')),
          targetStatus: 30
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Transition failed.');
          });
        return;
      }
      var invoice = e.target.closest('[data-ce-workshop-invoice]');
      if (invoice) {
        apiPost('/check-engine/workshop/RaiseJobInvoice', token, {
          jobId: Number(invoice.getAttribute('data-ce-workshop-invoice'))
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Invoice failed.');
          });
      }
    });

    var createBtn = root.querySelector('[data-ce-workshop-create]');
    if (createBtn) {
      createBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var vehicleId = Number(root.querySelector('[data-ce-workshop-vehicle]')?.value || 0);
        var labour = Number(root.querySelector('[data-ce-workshop-labour]')?.value || 0);
        apiPost('/check-engine/workshop/CreateJob', token, {
          workshopAccountId: state.accountId,
          labourEstimate: labour,
          vehicles: vehicleId ? [{ vehicleConfigurationId: vehicleId }] : []
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Create job failed.');
          });
      });
    }

    refresh();
  }

  function initFleet(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var vehiclesBody = root.querySelector('[data-ce-fleet-vehicles]');
    var approvalsBody = root.querySelector('[data-ce-fleet-approvals]');
    var state = { accountId: null };

    function renderVehicles(vehicles) {
      if (!vehiclesBody) return;
      vehicles = vehicles || [];
      if (!vehicles.length) {
        vehiclesBody.innerHTML = '<tr><td colspan="4">No vehicles registered.</td></tr>';
        return;
      }
      vehiclesBody.innerHTML = vehicles
        .map(function (v) {
          return (
            '<tr><td>' +
            (pick(v, 'id', 'Id') || '') +
            '</td><td class="ce-code">' +
            (pick(v, 'vin', 'Vin') || '—') +
            '</td><td>' +
            (pick(v, 'vehicleConfigurationId', 'VehicleConfigurationId') || '—') +
            '</td><td>' +
            (pick(v, 'assetTag', 'AssetTag') || '—') +
            '</td></tr>'
          );
        })
        .join('');
    }

    function renderApprovals(requests) {
      if (!approvalsBody) return;
      requests = requests || [];
      if (!requests.length) {
        approvalsBody.innerHTML = '<tr><td colspan="5">No approval requests.</td></tr>';
        return;
      }
      approvalsBody.innerHTML = requests
        .map(function (r) {
          var id = pick(r, 'id', 'Id');
          var status = pick(r, 'status', 'Status');
          var statusId = typeof status === 'number' ? status : pick(r, 'statusId', 'StatusId');
          var actions =
            statusId === 0
              ? '<button type="button" class="ce-btn ce-btn--primary" data-ce-fleet-approve="' +
                id +
                '">Approve</button> <button type="button" class="ce-btn ce-btn--ghost" data-ce-fleet-reject="' +
                id +
                '">Reject</button>'
              : '—';
          return (
            '<tr><td>#' +
            id +
            '</td><td>' +
            (pick(r, 'productId', 'ProductId') || '') +
            '</td><td>' +
            (pick(r, 'quantity', 'Quantity') || '') +
            '</td><td>' +
            statusBadge(statusId) +
            '</td><td>' +
            actions +
            '</td></tr>'
          );
        })
        .join('');
    }

    function refresh() {
      return apiGet('/check-engine/fleet/DashboardData', token)
        .then(function (data) {
          var account = pick(data, 'account', 'Account');
          state.accountId = pick(account, 'id', 'Id');
          renderVehicles(pick(data, 'vehicles', 'Vehicles'));
          renderApprovals(pick(data, 'approvalRequests', 'ApprovalRequests'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Failed to load fleet dashboard.');
        });
    }

    root.addEventListener('click', function (e) {
      var approve = e.target.closest('[data-ce-fleet-approve]');
      if (approve) {
        apiPost('/check-engine/fleet/DecideApprovalRequest', token, {
          requestId: Number(approve.getAttribute('data-ce-fleet-approve')),
          approve: true
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Approval failed.');
          });
        return;
      }
      var reject = e.target.closest('[data-ce-fleet-reject]');
      if (reject) {
        apiPost('/check-engine/fleet/DecideApprovalRequest', token, {
          requestId: Number(reject.getAttribute('data-ce-fleet-reject')),
          approve: false,
          rejectionReason: 'Rejected from portal'
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Rejection failed.');
          });
      }
    });

    var importBtn = root.querySelector('[data-ce-fleet-import]');
    if (importBtn) {
      importBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var raw = (root.querySelector('[data-ce-fleet-vins]')?.value || '').split(/\s+/).filter(Boolean);
        apiPost('/check-engine/fleet/ImportFleetVins', token, {
          fleetAccountId: state.accountId,
          vins: raw
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Import failed.');
          });
      });
    }

    refresh();
  }

  function initDealer(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var catalogBody = root.querySelector('[data-ce-dealer-catalog]');
    var claimsBody = root.querySelector('[data-ce-dealer-claims]');
    var state = { accountId: null, catalog: [] };

    function renderCatalog() {
      if (!catalogBody) return;
      if (!state.catalog.length) {
        catalogBody.innerHTML = '<tr><td colspan="5">No catalog items.</td></tr>';
        return;
      }
      catalogBody.innerHTML = state.catalog
        .map(function (item) {
          var pid = pick(item, 'productId', 'ProductId');
          return (
            '<tr><td>' +
            pid +
            '</td><td class="ce-code">' +
            (pick(item, 'sku', 'Sku') || '') +
            '</td><td>' +
            (pick(item, 'name', 'Name') || '') +
            '</td><td>' +
            (pick(item, 'dealerPrice', 'DealerPrice') || 0) +
            '</td><td><input type="number" min="0" class="ce-mp-input" data-ce-dealer-qty="' +
            pid +
            '" value="0" style="width:4rem" /></td></tr>'
          );
        })
        .join('');
    }

    function renderClaims(claims) {
      if (!claimsBody) return;
      claims = claims || [];
      if (!claims.length) {
        claimsBody.innerHTML = '<tr><td colspan="4">No warranty claims.</td></tr>';
        return;
      }
      claimsBody.innerHTML = claims
        .map(function (c) {
          return (
            '<tr><td>#' +
            (pick(c, 'id', 'Id') || '') +
            '</td><td class="ce-code">' +
            (pick(c, 'oemNumber', 'OemNumber') || '') +
            '</td><td>' +
            statusBadge(pick(c, 'status', 'Status')) +
            '</td><td>' +
            (pick(c, 'vehicleConfigurationId', 'VehicleConfigurationId') || '') +
            '</td></tr>'
          );
        })
        .join('');
    }

    function refresh() {
      return apiGet('/check-engine/dealer/DashboardData', token)
        .then(function (data) {
          var account = pick(data, 'account', 'Account');
          state.accountId = pick(account, 'id', 'Id');
          state.catalog = pick(data, 'catalog', 'Catalog') || [];
          renderCatalog();
          renderClaims(pick(data, 'warrantyClaims', 'WarrantyClaims'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Failed to load dealer dashboard.');
        });
    }

    var orderBtn = root.querySelector('[data-ce-dealer-order]');
    if (orderBtn) {
      orderBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var lines = [];
        root.querySelectorAll('[data-ce-dealer-qty]').forEach(function (input) {
          var qty = Number(input.value || 0);
          if (qty <= 0) return;
          var productId = Number(input.getAttribute('data-ce-dealer-qty'));
          var item = state.catalog.find(function (c) {
            return pick(c, 'productId', 'ProductId') === productId;
          });
          lines.push({
            productId: productId,
            quantity: qty,
            unitPrice: pick(item, 'dealerPrice', 'DealerPrice') || 0
          });
        });
        apiPost('/check-engine/dealer/PlaceDealerOrder', token, {
          dealerAccountId: state.accountId,
          lines: lines
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Order failed.');
          });
      });
    }

    var claimBtn = root.querySelector('[data-ce-dealer-claim]');
    if (claimBtn) {
      claimBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        apiPost('/check-engine/dealer/SubmitWarrantyClaim', token, {
          dealerAccountId: state.accountId,
          oemNumber: root.querySelector('[data-ce-dealer-oem]')?.value || '',
          vehicleConfigurationId: Number(root.querySelector('[data-ce-dealer-vehicle]')?.value || 0)
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Claim failed.');
          });
      });
    }

    refresh();
  }

  function initPortalAdmin(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    function provision(path) {
      var customerId = Number(root.querySelector('[data-ce-admin-customer]')?.value || 0);
      var displayName = root.querySelector('[data-ce-admin-name]')?.value || '';
      apiPost(path, token, { customerId: customerId, displayName: displayName })
        .then(function (res) {
          showAlert(alert, 'success', 'Provisioned ' + (res.portalKind || 'account') + ' #' + res.accountId);
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Provision failed.');
        });
    }
    root.querySelector('[data-ce-admin-workshop]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionWorkshop');
    });
    root.querySelector('[data-ce-admin-fleet]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionFleet');
    });
    root.querySelector('[data-ce-admin-dealer]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionDealer');
    });
  }

  function boot() {
    document.querySelectorAll('[data-ce-page="workshop"]').forEach(initWorkshop);
    document.querySelectorAll('[data-ce-page="fleet"]').forEach(initFleet);
    document.querySelectorAll('[data-ce-page="dealer"]').forEach(initDealer);
    document.querySelectorAll('[data-ce-page="portal-admin"]').forEach(initPortalAdmin);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();

  window.CheckEnginePortals = { initWorkshop: initWorkshop, initFleet: initFleet, initDealer: initDealer };
})(window);
