/**
 * emailJobs.js — Módulo de administración de lotes de correo (EmailJobs).
 * Expone EmailJobs.initIndex() y EmailJobs.initDetails().
 */
var EmailJobs = (function () {
    'use strict';

    // ─── Utilidades ───────────────────────────────────────────────────────────

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val() || '';
    }

    function postJson(url, onSuccess) {
        $.ajax({
            url:         url,
            type:        'POST',
            contentType: 'application/json',
            headers:     { RequestVerificationToken: getAntiForgeryToken() },
            success:     onSuccess,
            error: function (xhr) {
                var msg = 'Error de red o servidor (' + xhr.status + ').';
                try { msg = JSON.parse(xhr.responseText).message || msg; } catch (e) { /* */ }
                Swal.fire({ icon: 'error', title: 'Error', text: msg });
            }
        });
    }

    // ─── Index ───────────────────────────────────────────────────────────────

    function initIndex(indexUrl) {
        $('#tblJobs').DataTable({
            language:   { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
            order:      [[1, 'desc']],
            pageLength: 25,
            columnDefs: [{ orderable: false, targets: -1 }]
        });

        $('#btnRefresh').on('click', function () {
            window.location.reload();
        });
    }

    // ─── Details ──────────────────────────────────────────────────────────────

    function initDetails(jobId, isActive, detailsJsonUrl, retryJobUrl, retryItemUrl) {

        // DataTable (inicializar antes de adjuntar eventos — DT clona filas)
        var table = $('#tblItems').DataTable({
            language:   { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
            order:      [[5, 'asc']],
            pageLength: 50,
            columnDefs: [{ orderable: false, targets: 7 }]   // columna Acción no ordenable
        });

        // ── Botón "Reintentar fallidos del job" ───────────────────────────────
        $('#btnRetryAll').on('click', function () {
            Swal.fire({
                icon:              'warning',
                title:             '¿Reintentar todos los fallidos?',
                text:              'Los correos Failed y DeadLetter de este lote volverán a la cola de envío.',
                showCancelButton:  true,
                confirmButtonText: 'Sí, reintentar',
                cancelButtonText:  'Cancelar',
                confirmButtonColor: '#f39c12'
            }).then(function (result) {
                if (!result.isConfirmed) return;

                postJson(retryJobUrl, function (data) {
                    if (data.success) {
                        Swal.fire({
                            icon:  'success',
                            title: 'Reintento programado',
                            text:  data.message,
                            timer: 2500,
                            showConfirmButton: false
                        }).then(function () { window.location.reload(); });
                    } else {
                        Swal.fire({ icon: 'error', title: 'No se pudo reintentar', text: data.message });
                    }
                });
            });
        });

        // ── Botones "Reintentar este correo" (delegación para compatibilidad con DT) ─
        $(document).on('click', '.btn-retry-item', function () {
            var itemId = $(this).data('item-id');
            var email  = $(this).data('email');

            Swal.fire({
                icon:              'warning',
                title:             '¿Reintentar este correo?',
                html:              'Se reintentará el envío a <b>' + email + '</b>.',
                showCancelButton:  true,
                confirmButtonText: 'Sí, reintentar',
                cancelButtonText:  'Cancelar',
                confirmButtonColor: '#f39c12'
            }).then(function (result) {
                if (!result.isConfirmed) return;

                var url = retryItemUrl + '/' + itemId + '?jobId=' + jobId;
                postJson(url, function (data) {
                    if (data.success) {
                        Swal.fire({
                            icon:  'success',
                            title: 'Reintento programado',
                            text:  data.message,
                            timer: 2000,
                            showConfirmButton: false
                        }).then(function () { window.location.reload(); });
                    } else {
                        Swal.fire({ icon: 'error', title: 'No se pudo reintentar', text: data.message });
                    }
                });
            });
        });

        // ── Polling (solo cuando el job está activo) ───────────────────────────
        if (!isActive) return;

        var pollInterval = setInterval(function () {
            $.getJSON(detailsJsonUrl)
                .done(function (data) {
                    updateDetailsCards(data);

                    var done = data.status !== 'Accepted' && data.status !== 'Processing';
                    if (done) {
                        clearInterval(pollInterval);
                        updateStatusBadge(data.status);
                        $('.fa-spin').removeClass('fa-spin');
                        setTimeout(function () { window.location.reload(); }, 2000);
                    }
                })
                .fail(function () {
                    // silencio — no romper UI ante error de red puntual
                });
        }, 15000);
    }

    // ─── Helpers UI ──────────────────────────────────────────────────────────

    function updateDetailsCards(data) {
        var total  = data.totalItems  || 0;
        var sent   = data.sentCount   || 0;
        var failed = data.failedCount || 0;
        var pct    = total === 0 ? 0 : Math.round((sent + failed) * 100 / total);

        $('#sentCount').text(sent + ' / ' + total);
        $('#failedCount').text(failed);
        $('#progressBar').css('width', pct + '%');
        $('#progressPct').text(pct + '%');

        if (data.completedAt) {
            var d = new Date(data.completedAt);
            $('#completedAt').text(
                d.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
            );
        }
    }

    function updateStatusBadge(status) {
        var cssMap = {
            'Accepted':            'badge bg-info',
            'Processing':          'badge bg-warning text-dark',
            'Completed':           'badge bg-success',
            'CompletedWithErrors': 'badge bg-warning text-dark',
            'Failed':              'badge bg-danger',
            'Cancelled':           'badge bg-secondary'
        };
        var css = cssMap[status] || 'badge bg-secondary';
        $('#statusBadge').attr('class', css).text(status);
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    return {
        initIndex:   initIndex,
        initDetails: initDetails
    };
})();
