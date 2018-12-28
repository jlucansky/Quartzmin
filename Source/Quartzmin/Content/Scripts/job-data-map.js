var $jobDataMapRowCounter = 0;

function jobDataMapItem() {
    return this.each(function () {

        var row = $(this);
        var table = row.closest('table');

        function nameChanged(e) {

            if (e.target.value === "")
                return;

            var lastRow = table.find('>tbody>tr:last');

            if (row.is(lastRow)) {
                // add new row only if we editing the last row
                addItem();
            }
        }

        function addItem() {
            var elm = $jobDataMapRowTemplate.clone()
                .appendTo(table.find('>tbody'))
                .jobDataMapItem();
        }

        function deleteItem() {
            var visibleRows = table.find('>tbody>tr');

            if (visibleRows.length === 1) {
                addItem(); // add empty row
            }

            row.remove();
        }

        function cloneItem() {
            var elm = row.clone()
                .insertBefore(row)
                .jobDataMapItem();

            elm.find('.accept-error.error').each(function () { $(this).removeClass('error'); });
        }

        function setUniqueInputNames() {
            var rowIndex = row.data('row-index');
            $(this).find('[name]').each(function () {
                $name = $(this).attr('name');

                var n = $name.lastIndexOf(':');
                if (n !== -1)
                    $name = $name.substring(0, n);

                $(this).attr('name', $name + ':' + rowIndex); // ensure every input has unique name
            });
        }

        function initValueElement() {
            var valueCol = row.find('.value-col');
            setUniqueInputNames.call(valueCol);

            var typeName = row.find('.type-col .selected-type').val();
            row.find('.type-col').data('selected-type', typeName); // set current

            var typeId = row.find('.type-col .menu .item').filter("[data-value='" + typeName + "']").data('type-handler-id');

            valueCol.wrapInner("<div class='value-container'></div>");

            // call custom initializer (based on selected type handler)
            var fn = $typeHandlerScripts[typeId];
            if (fn)
                fn.call(valueCol, 'init');
        }

        function typeChanged(value) {
            var typeDropdown = row.find('.type-col .ui.dropdown');

            if (typeDropdown.hasClass('disabled'))
                return;

            typeDropdown.dropdown('hide').addClass('disabled');

            var valueCol = row.find('.value-col');
            var typeName = value;
            var prevTypeName = row.find('.type-col').data('selected-type');
            var allTypes = row.find('.type-col .menu .item');
            var typeHandlerTarget =  allTypes.filter("[data-value='" + typeName + "']").data('type-handler');
            var typeHandlerCurrent = allTypes.filter("[data-value='" + prevTypeName + "']").data('type-handler');
            var valueContainer = valueCol.find('>.value-container');
            var innerNames = valueContainer.find('[name]');

            valueContainer.removeClass('transition visible');
            valueContainer.hide();

            var loader = $('<div class="ui active inline small loader"/>');
            valueCol.append(loader);
            loader.transition('fade in');

            var formData = new FormData();

            formData.append('selected-type', typeHandlerCurrent);
            formData.append('target-type', typeHandlerTarget);

            innerNames.each(function () {
                if ($(this).is('select')) {
                    formData.append(this.name, $(this).val());
                }
                if ($(this).is('textarea')) {
                    formData.append(this.name, $(this).val());
                }
                if ($(this).is('input')) {
                    if (this.type.toLowerCase() === 'file')
                        formData.append(this.name, this.files.length > 0 ? this.files[0] : '');
                    else
                        formData.append(this.name, this.value);
                }
            });

            function cleanup(value) {
                typeDropdown.removeClass('disabled');
            }

            $.ajax({
                type: 'POST', enctype: 'multipart/form-data', url: $changeTypeCallbackUrl,
                data: formData, processData: false, contentType: false, cache: false,
                timeout: 15000,
                success: function (data) {
                    cleanup(typeName);
                    valueCol.empty().append(data);
                    initValueElement();
                    valueCol.find('>.value-container').transition('fade in');
                    typeDropdown.removeClass('error');
                },
                error: function (e) {
                    loader.remove();
                    typeDropdown.dropdown('set selected', prevTypeName);
                    valueContainer.show();
                    cleanup(prevTypeName);

                    initErrorMessage.call($(getErrorMessage(e)).insertBefore(table), typeDropdown);

                    typeDropdown.addClass('error');
                }
            });

        }

        function prepareForm() {
            // copy selected type handler to hidden input field
            var typeName = row.find('.type-col').data('selected-type');
            var typeHandler = row.find('.type-col .menu .item').filter("[data-value='" + typeName + "']").data('type-handler');
            row.find(".type-col input.type-handler").val(typeHandler);
        }

        // init components
        row.data('row-index', ++$jobDataMapRowCounter);
        row.find('.ui.dropdown').dropdown();
        setUniqueInputNames.call(row.find('.name-col, .type-col'));
        initValueElement();

        // event handlers
        row.find('.name-col input').on('input', nameChanged);
        row.find('.type-col .ui.dropdown').dropdown('setting', 'onChange', typeChanged);
        row.find('.delete-row').click(deleteItem);
        row.find('.copy-row').click(cloneItem);
        row.find('.type-col').data('prepare-form', prepareForm);
    });
}

var $jobDataMapRowTemplate;

$(function() {
    $jobDataMapRowTemplate = $('#job-data-map>tbody>tr.template').detach().removeClass('template');

    $.fn.jobDataMapItem = jobDataMapItem;
    $('#job-data-map>tbody>tr').jobDataMapItem();

    $.fn.jobDataMapPrepareForm = function () {
        $("input.last-data-item", this).remove();
        var $lastRow = $(">tbody>tr:last-child", this);
        var $rowIndex = $lastRow.data('row-index');
        $lastRow.find('.name-col').append('<input type="hidden" class="last-data-item" name="data-map[lastItem]:' + $rowIndex + '" value="True" />');
        $(this).find('.type-col').each(function () { $(this).data('prepare-form')(); });
    };
});
