const gulp = require('gulp');
const concat = require('gulp-concat');
const terser = require('gulp-terser');
const cleanCSS = require('gulp-clean-css');

// Bundle and minify JavaScript
gulp.task('js', () => {
    return gulp.src(['wwwroot/js/site.js', 'wwwroot/js/clock-animation.js', 'wwwroot/js/invoice-creation.js', 'wwwroot/js/filters.js', 'wwwroot/js/expenses.js', 'wwwroot/js/timeentries.js'])
        .pipe(concat('site.min.js'))
        .pipe(terser({
            mangle: { toplevel: true },
            compress: true
        }))
        .pipe(gulp.dest('wwwroot/js'));
});

// Bundle and minify CSS
gulp.task('css', () => {
    return gulp.src(['wwwroot/css/site.css', 'wwwroot/css/clock-animation.css', 'wwwroot/css/invoices.css', 'wwwroot/css/filters.css', 'wwwroot/css/layout.css', 'wwwroot/css/timeentries.css', 'wwwroot/css/dropdown.css'])
        .pipe(concat('site.min.css'))
        .pipe(cleanCSS({ compatibility: 'ie8' }))
        .pipe(gulp.dest('wwwroot/css'));
});

// Default task: Run both js and css tasks
gulp.task('default', gulp.parallel('js', 'css'));

// Watch task for development
gulp.task('watch', () => {
    gulp.watch([
        'wwwroot/js/site.js',
        'wwwroot/js/clock-animation.js',
        'wwwroot/css/site.css',
        'wwwroot/css/clock-animation.css',
        'wwwroot/css/layout.css'
    ], { ignoreInitial: false }, gulp.series('default'));
});