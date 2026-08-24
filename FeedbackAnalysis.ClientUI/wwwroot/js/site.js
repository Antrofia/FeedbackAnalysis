// Раскрытие/скрытие формы ответа на карточке отзыва
$(document).on('click', '.reply-toggle', function () {
    var reviewId = $(this).data('review-id');
    $('.reply-form[data-review-id="' + reviewId + '"]').toggle();
});
