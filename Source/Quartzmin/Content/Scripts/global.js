if (!String.prototype.startsWith) {
    String.prototype.startsWith = function (searchString, position) {
        position = position || 0;
        return this.indexOf(searchString, position) === position;
    };
}

if (!Array.prototype.fill) {
    Object.defineProperty(Array.prototype, 'fill', {
        value: function (value) {

            // Steps 1-2.
            if (!this) {
                throw new TypeError('this is null or not defined');
            }

            var O = Object(this);

            // Steps 3-5.
            var len = O.length >>> 0;

            // Steps 6-7.
            var start = arguments[1];
            var relativeStart = start >> 0;

            // Step 8.
            var k = relativeStart < 0 ?
                Math.max(len + relativeStart, 0) :
                Math.min(relativeStart, len);

            // Steps 9-10.
            var end = arguments[2];
            var relativeEnd = end === undefined ?
                len : end >> 0;

            // Step 11.
            var final = relativeEnd < 0 ?
                Math.max(len + relativeEnd, 0) :
                Math.min(relativeEnd, len);

            // Step 12.
            while (k < final) {
                O[k] = value;
                k++;
            }

            // Step 13.
            return O;
        }
    });
}

function getErrorMessage(e) {
    var statusNum = '';
    var statusText = e.statusText;

    if (e.responseJSON && e.responseJSON.ExceptionMessage)
        statusText = e.responseJSON.ExceptionMessage;

    if (e.status > 0)
        statusNum = ' (' + e.status + ')';

    const msg =
        '<div class="ui negative message">' +
        '<i class="close icon"></i>' +
        '<div class="header">An error occured' + statusNum +
        '</div><p>' + statusText + '</p></div>';

    return msg;
}

function initErrorMessage(errorElements) {
    $(this)
        .transition('fade in')
        .find('.close')
        .on('click', function () {
            if (errorElements) {
                errorElements.removeClass('error');
            }
            $(this).closest('.message').transition('fade');
        });

    return $(this);
}

function prependErrorMessage(e, parent) {
    
    parent.prepend(initErrorMessage.call($(getErrorMessage(e))));
}

function initDimmer() {
    return $('#dimmer').dimmer({ closable: false, duration: 100, opacity: 1 });
}

function deleteItem(key, msgParent, delUrl, redirUrl) {

    $('#delete-dialog')
        .modal({
            duration: 250,
            onApprove: function () {

                $('#dimmer').dimmer('show');

                $.ajax({
                    type: 'POST', url: delUrl,
                    data: JSON.stringify(key),
                    contentType: 'application/json', cache: false,
                    success: function () {
                        document.location = redirUrl;
                    },
                    error: function (e) {
                        $('#dimmer').dimmer('hide');
                        prependErrorMessage(e, msgParent);
                    }
                });

            }
        })
        .modal('show');
}

function initHistogramTooltips(elements) {
    elements.each(function () {
        const tooltip = $(this).data('tooltip-html');
        if (tooltip) {
            $(this).popup({
                html: '<div class="histogram-tooltip">' + tooltip + '</div>',
                hoverable: true,
                transition: 'fade',
                delay: 200
            });
        }
    });
}

function initCronLiveDescription(url, $cronInput, $cronDesc, $nextCronDates) {
    function describeCron() {
        $.ajax({
            type: 'POST', url: url, timeout: 5000,
            data: $cronInput.val(), contentType: 'text/plain', dataType: 'json',
            success: function (data) {
                $cronDesc.text(data.Description);
                var nextHtml = data.Next.join('<br>');
                if (nextHtml === '') $nextCronDates.hide(); else {
                    $nextCronDates.show();
                    $nextCronDates.popup({ html: '<div class="header">Scheduled dates</div><div class="content">' + nextHtml + '</div>' });
                }
            },
            error: function (e) { $cronDesc.text('Error occured.'); }
        });
    }
    var cronDescTimer;
    $cronInput.on('input', function (e) {
        window.clearTimeout(cronDescTimer);
        searchcronDescTimerTimer = window.setTimeout(function () {
            cronDescTimer = null;
            describeCron();
        }, 250);
    });

    describeCron();
}

function loadAdditionalData(rowIndex, url) {
    function applyAdditionalData(element) {
        const row = rowIndex[element.data('row')];
        if (row) {
            element.find('>td').each(function () {
                row.find('.' + $(this).attr('class')).html($(this).html());
            });
        }
    }

    $.ajax({ // obtain additional data on background - this can take longer
        url: url, dataType: "html", cache: false,
        success: function (data) {
            $(data).find('>tbody>tr').each(function () {
                applyAdditionalData($(this));
            });
            initHistogramTooltips($('.histogram > .bar'));
        }
    });
}
