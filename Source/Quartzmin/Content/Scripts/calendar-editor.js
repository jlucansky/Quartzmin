function initQuartzCalendarEditor(options) {

    const $_ = $(this);

    let calendarConfig = {
        'none': {
            name: '--- Not Set ---',
            isNone: true
        },
        'annual': {
            name: 'Annual',
            html: function (model) {
                return [createInfoMessage('The calendar excludes a set of days of the year.' +
                        '<p>You may use it to exclude bank holidays which are on the same date every year.</p>')]
                    .concat(multiDatePicker('monthday', 'Days', 'Day', 'Exclude Days', model, model.Days));
            },
            prepareModel: function (model) { model.Days = []; }
        },
        'cron': {
            name: 'Cron',
            html: function (model) {
                return [
                    createInfoMessage('The calendar excludes the set of times expressed by a given <b>Cron Expression</b>.' +
                        '<p>For example, you could use this calendar to exclude all but business hours (8:00 - 17:00) every day using the expression <span style="white-space: nowrap; font-family: monospace; font-weight:bold; background-color: #fff; border: 1px solid #e3e3e8; padding: 2px 4px">* * 0-7,18-23 ? * *</span></p>' +
                        '<p>It is important to remember that the cron expression here describes a set of times to be excluded from firing. Whereas the cron expression in Cron Trigger describes a set of times that can be included for firing. Thus, if a Cron Trigger has a given cron expression and is associated with a Cron Calendar with the same expression, the calendar will exclude all the times the trigger includes, and they will cancel each other out.</p>'),
                    createDescriptionField(model),
                    createLiveCronField('CronExpression', 'Cron Expression', model.CronExpression),
                    createTimezoneField(model.TimeZone)
                ];
            }
        },
        'daily': {
            name: 'Daily',
            html: function (model) {
                return [
                    createInfoMessage('The calendar excludes (or includes when inverted) a specified time range each day.' +
                        '<p>For example, you could use this calendar to exclude business hours (8:00 - 17:00) every day. Each Daily Calendar only allows a single time range to be specified, and that time range may not cross daily boundaries (i.e. you cannot specify a time range from 16:00 - 5:00).</p>' +
                        '<ul><li>If <b>Invert Time Range</b> is unchecked, the time range defines a range of times in which triggers are not allowed to fire.</li>' +
                        '<li>If <b>Invert Time Range</b> is checked, the time range is inverted: that is, all times outside the defined time range are excluded.</li></ul>')
                    , createDescriptionField(model),
                $('<div class="fields">')
                    .append($('<div class="four wide field accept-error">')
                        .append([$('<label>').text('Starting Time'), createDateInput('time', 'StartingTime', 'Time', model.StartingTime)]))
                    .append($('<div class="four wide field accept-error">')
                        .append([$('<label>').text('Ending Time'), createDateInput('time', 'EndingTime', 'Time', model.EndingTime)]))
                    ,
                $('<div class="field">')
                    .append($('<div class="ui checkbox">')
                        .append('<input name="InvertTimeRange" type="checkbox" value="True"><label>Invert Time Range</label>')
                        .checkbox(model.InvertTimeRange ? 'check' : 'uncheck')
                    ), createTimezoneField(model.TimeZone)];
            }
        },
        'holiday': {
            name: 'Holiday',
            html: function (model) {
                return [createInfoMessage('The calendar stores a list of holidays (full days that are excluded from scheduling).' +
                    '<p>The calendar does take the year into consideration, so if you want to exclude July 4th for the next 10 years, you need to add 10 entries to the exclude list.</p>')]
                .concat(multiDatePicker('date', 'Dates', 'Date', 'Exclude Dates', model, model.Dates));
            },
            prepareModel: function (model) { model.Dates = []; }
        },
        'monthly': {
            name: 'Monthly',
            html: function (model) {

                const $grid = $('<table class="ui unstackable very basic single line table monthly">');
                $grid.append('<tr><td style="text-align: right" colspan="7"><a href="javascript:void(0)" class="btn-invert">Invert</a> | <a href="javascript:void(0)" class="btn-clear">Clear</a></td></tr>');
                let c = 0;
                for (let i = 0; i < 5; i++) {
                    var $row = $('<tr>');
                    for (let n = 0; n < 7 && c < 31; n++) {
                        const active = model.DaysExcluded && model.DaysExcluded[c] ? 'active' : '';
                        $row.append($('<td>')
                            .append($('<div class="ui tiny toggle button ' + active + '">').text(c + 1).data('idx', c)));

                        c++;
                    }
                    $grid.append($row);
                }

                $grid.on('click', '.toggle', function () {
                    if ($(this).hasClass('active'))
                        $(this).removeClass('active');
                    else
                        $(this).addClass('active');
                });

                $('.btn-invert', $grid).click(function () {
                    $(':not(.active).toggle', $grid).addClass('mark-activate');
                    $('.active.toggle', $grid).removeClass('active');
                    $('.mark-activate', $grid).removeClass('mark-activate').addClass('active');
                });

                $('.btn-clear', $grid).click(function () { $('.active.toggle', $grid).removeClass('active'); });

                return [
                    createInfoMessage('The calendar excludes a set of days of the month.' +
                        '<p>You may use it to exclude every 1. of each month for example. But you may define any day of a month.</p>'),

                    createDescriptionField(model),
                    $('<div class="fields">').append($('<div class="field">').append([
                        $('<label>').text('Exclude Days'), $grid])),
                    createTimezoneField(model.TimeZone)
                ];
            },
            prepareModel: function (model) {
                model.DaysExcluded = new Array(31).fill(false, 0, 31);
                $('.monthly .toggle.active', this).each(function () { model.DaysExcluded[$(this).data('idx')] = true; });
            }
        },
        'weekly': {
            name: 'Weekly',
            html: function (model) {
                const $grid = [];
                const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
                for (let i in days) {
                    const n = (Number(i) + 1) % 7; // sunday is 0
                    const active = model.DaysExcluded && model.DaysExcluded[n] ? 'active' : '';
                    $grid.push($('<div class="ui tiny toggle button '+active+'">').text(days[i]).data('idx', n));
                }

                return [
                    createInfoMessage('The calendar excludes a set of days of the week.' +
                        '<p>You may use it to exclude weekends for example. But you may define any day of the week.</p>'),

                    createDescriptionField(model),
                    $('<div class="fields">').append($('<div class="field weekly">')
                        .append($('<label>').text('Exclude Days'))
                        .append($grid)
                        .on('click', '.toggle', function () {
                            if ($(this).hasClass('active'))
                                $(this).removeClass('active');
                            else
                                $(this).addClass('active');
                        })
                    ),
                    createTimezoneField(model.TimeZone)
                ];
            },
            prepareModel: function (model) {
                model.DaysExcluded = new Array(7).fill(false, 0, 7);
                $('.weekly .toggle.active', this).each(function () { model.DaysExcluded[$(this).data('idx')] = true; });
            }
        },
        'custom': {
            name: 'Custom',
            isCustom: true,
            html: function (model) {
                return [
                    createDescriptionField(model),
                    createTextField('CustomType', 'Fully Qualified Type Name', model.CustomType, undefined, 'disabled')
                ];
            }
        }
    };

    $(document).on('click', '.delete-field', function () { $(this).closest('.field').remove(); });

    function multiDatePicker(mode, name, placeholder, label, model, list) {
        const actions = '<a href="javascript:void(0)" class="delete-field" title="Delete"><i class="red large trash alternate outline icon"></i></a>';
        const elm = [];
        if (list) {
            for (let i in list)
                elm.push(createFloatDateField(mode, name, placeholder, list[i], actions));
        }
        elm.push(createFloatDateField(mode, name, placeholder, '', actions));
        return [
            createDescriptionField(model),
            $('<div class="wrapped fields">')
                .append($('<div class="field">').append($('<label>').text(label)))
                .append(elm)
                .on('focus', '.field:last-child input', function () {
                    $(this).closest('.fields').append(createFloatDateField(mode, name, placeholder, '', actions));
                })
            , createTimezoneField(model.TimeZone)
        ];
    }

    function createInfoMessage(content) {
        return $('<div class="ui message">')
            .append('<i class="info circle icon">')
            .append(content);
    }

    function createDescriptionField(model) {
        return createTextField('Description', 'Description', model.Description, 'ten');
    }

    function createTextField(name, label, value, width, attrs) {
        if (!width) width = 'eight';
        if (!attrs) attrs = '';
        return $('<div class="fields">')
            .append($('<div class="'+width+' wide field accept-error">')
                .append($('<label>').text(label))
                .append($('<input type="text"' + attrs + '>')
                    .attr({
                        name: name,
                        value: value,
                        placeholder: label
                    })
                )
            );
    }

    function calendarSettings(mode) {

        switch (mode) {
            case 'monthday':
                return {
                    type: 'date',
                    disableYear: true,
                    startMode: 'month',
                    formatter: {
                        date: function (date) {
                            return moment(date).format("MMMM D");
                        },
                        header: function (date, mode) {
                            if (mode === 'day')
                                return moment(date).format("MMMM");
                            if (mode === 'month')
                                return 'Select Month';
                        }
                    }
                };
            case 'date':
                return {
                    type: 'date',
                    formatter: { date: function (date) { return moment(date).format(options.dateFormat); } },
                    parser: { date: function (text) { return moment(text, options.dateFormat).toDate(); } }
                };
            case 'time':
                return {
                    type: 'time',
                    ampm: false,
                    formatInput: false,
                    formatter: {
                        time: function (date) { return moment(date).format('HH:mm:ss'); }
                    }
                };
            default:
                throw "Invalid calendar mode.";
        }
       
    }

    function createFloatDateField(mode, name, label, value, actions) {
        return $('<div class="wrapped field accept-error">').append(createDateInput(mode, name, label, value, actions));
    }

    function createDateInput(mode, name, label, value, actions) {

        // mode: monthday, time, day

        let icon = 'calendar alternate outline';
        if (mode === 'time')
            icon = 'clock alternate outline';
        
        const $field = $('<div class="ui calendar">')
            .append($('<div class="ui input left icon">')
                .append($('<i class="' + icon + ' icon">'))
                .append($('<input type="text" autocomplete="off">')
                    .attr({
                        name: name,
                        value: value,
                        placeholder: label
                    }))
                .append(actions)
        ).calendar(calendarSettings(mode));

        return $field;
    }

    function createLiveCronField(name, label, value, width) {
        if (!width) width = 'eight';

        const $field = $('<div class="' + width + ' wide field accept-error cron-field">')
            .append([
                $('<label>' + label + ' <a href="http://cronmaker.com" target="_blank"><i class="external alternate icon"></i>http://cronmaker.com</a></label>'),
                $('<input type="text" class="cron-expression">')
                    .attr({
                        name: name,
                        value: value,
                        placeholder: label
                    }),
                $('<div style="float: right; cursor:pointer; display: none" class="next-cron-dates"><i class="eye icon"></i></div>'),
                $('<p class="cron-desc"></p>')
            ]);

        initCronLiveDescription(options.cronUrl, $field.find('.cron-expression'), $field.find('.cron-desc'), $field.find('.next-cron-dates'));

        return $('<div class="fields">').append($field);
    }

    function createCalendarContent(model) {
        const cnt = $('<div class="cal-content">');
        var t = calendarConfig[model.Type];
        if (!t) t = calendarConfig.none;
        if (t.html) cnt.append(t.html(model));
        cnt.data('config', t);
        return cnt;
    }

    function calendarTypeChanged(value) {
        const
            $seg = $(this).closest('.segment'),
            $calContent = $seg.find('>.cal-content'),
            desc = $calContent.find('input[name=Description]').val(),
            model = $seg.data('model');

        let tz = $calContent.find('select[name=TimeZone]').val();

        if (tz === undefined) tz = options.defaultTimezone;

        model.Type = value;
        model.Description = desc;
        model.TimeZone = tz;
        model.DaysExcluded = null;

        $calContent.replaceWith(createCalendarContent(model));

        if (value === 'none') {
            const $ss = $(this).closest('.segments').find('>.segment');
            for (let i = $ss.length - 1; i > 0; i--) {
                const elem = $ss[i];
                if ($seg[0] === elem)
                    break;
                elem.parentNode.removeChild(elem);
            }
        } else {
            if ($seg[0] === $(this).closest('.segments').find('>.segment:last-child')[0]) {
                appendSegment({Type: 'none'});
            }
        }
    }

    function createCalendarTypeField(model) {
        const choices = [], keys = Object.keys(calendarConfig);
        for (let i in keys) {
            const key = keys[i], obj = calendarConfig[key];
            if (model.IsRoot && obj.isNone) continue;
            if (obj.isCustom && !model.CustomType) continue;
            choices.push($('<option>', { value: key, selected: model.Type === key }).text(obj.name));
        }
        return $('<div class="fields">')
            .append($('<div class="six wide field accept-error">')
                .append($('<label>').text(model.IsRoot ? 'Calendar Type' : 'Calendar Base Type'))
                .append($('<select class="ui fluid dropdown" name="Type">')
                    .append(choices)
                    .dropdown({ onChange: calendarTypeChanged })
                )
            );
    }

    function createTimezoneField(selected) {
        const choices = [], keys = Object.keys(options.timezones);
        choices.push($('<option>', { value: '', selected: selected === '' }).text('--- Not Set ---'));
        for (let i in keys) {
            const key = keys[i], txt = options.timezones[key];
            choices.push($('<option>', { value: key, selected: selected === key }).text(txt));
        }
        return $('<div class="fields">')
            .append($('<div class="ten wide field accept-error">')
                .append($('<label>').text('Time Zone'))
                .append($('<select class="ui fluid search selection dropdown" name="TimeZone">')
                    .append(choices)
                    .dropdown({ placeholder: 'false', fullTextSearch: 'exact' })
                )
            );
    }

    function appendSegment(model) {
        const segment = $('<div class="ui segment">').data('model', model);

        if (model.IsRoot) {
            segment.append(createTextField('Name', 'Name', model.Name, 'six', options.isNew ? '' : 'disabled'));
        }

        segment.append([
            createCalendarTypeField(model),
            createCalendarContent(model)]);

        $_.append(segment);
    }

    function updateModel() {
        const
            $seg = $(this),
            model = $seg.data('model'),
            cfg = $seg.find('>.cal-content').data('config');

        if (cfg.prepareModel) cfg.prepareModel.call($seg, model);

        $('input[name]', this).each(function () {
            const name = $(this).attr('name'), type = $(this).attr('type'), isArray = $(this).closest('.wrapped.fields').length;
            let val = $(this).val();

            if (type === 'checkbox') val = $(this)[0].checked;

            if (isArray) {
                if (model[name]) model[name].push(val); else model[name] = [val];
            } else
                model[name] = val;
        });

        $('select[name]', this).each(function () { model[$(this).attr('name')] = $(this).val(); });

        return model;
    }

    $_.data('getModel', function () {
        const result = [];
        $_.find('>.segment').each(function () { result.push(updateModel.call(this)); });
        return result;
    });

    for (let i = 0; i < options.model.length; i++) {
        appendSegment(options.model[i]);
    }
    appendSegment({Type: 'none'});
}
