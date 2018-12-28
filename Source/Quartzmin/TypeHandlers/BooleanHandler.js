function init() {
    var $input = $('input', this);

    var $toggle = $('.ui.toggle.button', this)
        .state({ text: { inactive: 'False', active: 'True' } })
        .click(function () { $input.val($toggle.text()); });

    if ($input.val().toLowerCase() === 'true') {
        $toggle.text('True');
        $toggle.addClass('active');
    } else {
        $toggle.text('False');
    }
}