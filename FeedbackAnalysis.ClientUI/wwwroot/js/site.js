// Раскрытие/скрытие формы ответа на карточке отзыва.
// Открывается только одна форма одновременно; фокус сразу в текстовое поле.
$(document).on('click', '.reply-toggle', function () {
    var reviewId = $(this).data('review-id');
    var $panel = $('.reply-form[data-review-id="' + reviewId + '"]');
    var wasOpen = $panel.is(':visible');

    $('.reply-form').stop(true, true).hide();
    $('.reply-toggle').attr('aria-expanded', 'false');

    if (!wasOpen) {
        $panel.stop(true, true).slideDown(120);
        $(this).attr('aria-expanded', 'true');
        $panel.find('textarea[name="text"]').trigger('focus');
    }
});
