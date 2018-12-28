function init() {
    var
        $cal = $('.ui.calendar', this),
        dateFmt = $cal.data('date-format').toUpperCase(),
        timeFmt = $cal.data('time-format'),
        fmt = dateFmt + ' ' + timeFmt,
        calType = 'datetime';

    if ($cal.data('date-only').toLowerCase() === 'true')
        calType = 'date';

    $cal.calendar({
        type: calType,
        ampm: false,
        formatter: {
            date: function (date, settings) {
                return moment(date).format(dateFmt);
            },
            time: function (date, settings) {
                return moment(date).format(timeFmt);
            }
        },
        parser: {
            date: function (text, settings) {
                return moment(text, fmt).toDate();
            }
        }
    });
}