function init() {
    $('input:text', this).click(function () {
        $(this).closest('.value-container').find('input:file').click();
    });

    $('.download-button', this).click(function () {
        const data_b64 = $(this).closest('.value-container').find('textarea').val();

        function b64toBlob(b64Data, contentType, sliceSize) {
            contentType = contentType || '';
            sliceSize = sliceSize || 512;

            var byteCharacters = atob(b64Data);
            var byteArrays = [];

            for (var offset = 0; offset < byteCharacters.length; offset += sliceSize) {
                var slice = byteCharacters.slice(offset, offset + sliceSize);

                var byteNumbers = new Array(slice.length);
                for (var i = 0; i < slice.length; i++) {
                    byteNumbers[i] = slice.charCodeAt(i);
                }

                var byteArray = new Uint8Array(byteNumbers);

                byteArrays.push(byteArray);
            }

            var blob = new Blob(byteArrays, { type: contentType });
            return blob;
        }

        FileSaver.saveAs(b64toBlob(data_b64, 'application/octet-stream'), "file");
    });

    $('input:file', '.ui.input', this)
        .on('change', function (e) {

            const container = $(this).closest('.value-container');
            container.find('.old-file').remove();
            container.find('.file-selector').show();

            var name = e.target.files[0].name;
            $('input:text', $(e.target).parent()).val(name);
        });
}