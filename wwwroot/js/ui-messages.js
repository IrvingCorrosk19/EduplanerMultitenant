/**
 * Mensajes para el usuario con SweetAlert2 (toast y modales).
 * Requiere Swal global (sweetalert2 cargado antes que este script).
 */
(function (w) {
    'use strict';

    var primary = '#2563eb';
    var neutral = '#64748b';

    function hasSwal() {
        return typeof w.Swal !== 'undefined' && w.Swal.fire;
    }

    w.SchoolUi = w.SchoolUi || {};

    /** Notificación breve arriba a la derecha */
    w.SchoolUi.toastSuccess = function (title) {
        if (!hasSwal()) return Promise.resolve();
        return w.Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: title,
            showConfirmButton: false,
            timer: 3400,
            timerProgressBar: true
        });
    };

    w.SchoolUi.toastInfo = function (title) {
        if (!hasSwal()) return Promise.resolve();
        return w.Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'info',
            title: title,
            showConfirmButton: false,
            timer: 4000,
            timerProgressBar: true
        });
    };

    function modal(icon, title, text) {
        if (!hasSwal()) {
            w.alert((text ? title + '\n\n' + text : title) || '');
            return Promise.resolve();
        }
        return w.Swal.fire({
            icon: icon,
            title: title,
            text: text || undefined,
            confirmButtonText: 'Entendido',
            confirmButtonColor: primary
        });
    }

    w.SchoolUi.error = function (title, text) {
        return modal('error', title || 'No se pudo completar', text);
    };

    w.SchoolUi.warning = function (title, text) {
        return modal('warning', title || 'Atención', text);
    };

    w.SchoolUi.info = function (title, text) {
        return modal('info', title, text);
    };

    /** Confirmación con botón de acción en rojo (p. ej. eliminar) */
    w.SchoolUi.confirmDanger = function (title, text) {
        if (!hasSwal()) {
            return Promise.resolve({ isConfirmed: w.confirm(text || title) });
        }
        return w.Swal.fire({
            icon: 'warning',
            title: title,
            text: text || undefined,
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc2626',
            cancelButtonColor: neutral,
            reverseButtons: true
        });
    };

    /** Confirmación neutra */
    w.SchoolUi.confirm = function (title, text, confirmText) {
        if (!hasSwal()) {
            return Promise.resolve({ isConfirmed: w.confirm(text || title) });
        }
        return w.Swal.fire({
            icon: 'question',
            title: title,
            text: text || undefined,
            showCancelButton: true,
            confirmButtonText: confirmText || 'Sí',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: primary,
            cancelButtonColor: neutral
        });
    };
})(window);
