
function processValidationResponse(data, segmentRoot) {

    $('.accept-error.error').each(function () { $(this).removeClass('error'); });

    function cleanup() {
        $(this)
            .off('focusin', cleanup)
            .removeClass('error');
    }

    if (data.Success === false) {

        for (i = 0; i < data.Errors.length; i++) {
            var
                err = data.Errors[i],
                field = err.Field,
                errElm;

            if (field.startsWith('data-map[value]')) {
                var n = field.lastIndexOf(':');
                if (n === -1)
                    continue;
                var rowId = field.substring(n);
                errElm = $("[name='data-map[name]" + rowId + "']").closest('tr').find('.value-col.accept-error');
            } else {
                if (segmentRoot && (err.SegmentIndex > 0 || err.SegmentIndex === 0)) {
                    errElm = $('.segment:nth-child(' + (err.SegmentIndex + 1) + ') ' + "[name='" + field + "']", segmentRoot);
                } else {
                    errElm = $("[name='" + field + "']");
                }

                if (err.FieldIndex > 0 || err.FieldIndex === 0)
                    errElm = $(errElm[err.FieldIndex]);

                if (!errElm.hasClass('accept-error'))
                    errElm = errElm.closest('.accept-error');
            }

            errElm.addClass('error');
            errElm.on('focusin', cleanup);

            //errElm.popup({
            //    content: err.Reason,
            //    hoverable: true,
            //    position: 'top left',
            //    variation: 'inverted',
            //    distanceAway: -20
            //});
        }
    }

    return data.Success;
}