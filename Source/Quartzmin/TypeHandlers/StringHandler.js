function init() {
    var
        $modal = $('.modal', this),
        $txtHiddenField = $('textarea[name]', this),
        $txtModalField = $('.modal textarea', this),
        $stats = $('input.stats', this);

    function calcStats() {
        var
            str = $txtHiddenField[0].value,
            lines = str.split(/\r?\n|\r/).length;
        if (str.length === 0) lines = 0;
        $stats.val(str.length + ' bytes, ' + lines + ' lines');
    }

    if ($modal.length > 0) { // is multiline string
        // init
        $modal.modal({
            onShow: function () { $txtModalField.val($txtHiddenField.val()); },
            onApprove: function () { $txtHiddenField.val($txtModalField.val()); calcStats(); },
            duration: 250
        });

        calcStats();

        $('.edit-button', this).click(function () {
            $modal.modal('show');
        });
    }
}