﻿const clockIcons = [
    '~/images/icons/clock/clock-twelve.svg',
    '~/images/icons/clock/clock-one.svg',
    '~/images/icons/clock/clock-two.svg',
    '~/images/icons/clock/clock-three.svg',
    '~/images/icons/clock/clock-four.svg',
    '~/images/icons/clock/clock-five.svg',
    '~/images/icons/clock/clock-six.svg',
    '~/images/icons/clock/clock-seven.svg',
    '~/images/icons/clock/clock-eight.svg',
    '~/images/icons/clock/clock-nine.svg',
    '~/images/icons/clock/clock-ten.svg',
    '~/images/icons/clock/clock-eleven.svg'
].map(path => path.replace('~', window.AppSettings.BasePath || ''));

function animateClock(element) {
    if (!element) {
        console.warn('animateClock: Element is null');
        return;
    }
    let index = 0;
    element.src = clockIcons[index];
    setInterval(() => {
        index = (index + 1) % clockIcons.length;
        element.src = clockIcons[index];
    }, 300);
}

document.addEventListener('DOMContentLoaded', () => {
    console.log('clock-animation.js: Setting up MutationObserver');
    const observer = new MutationObserver((mutations) => {
        console.log('MutationObserver triggered', mutations);
        mutations.forEach((mutation) => {
            if (mutation.addedNodes.length) {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        if (node.classList.contains('clock-icon-animated')) {
                            console.log('Found clock-icon-animated', node);
                            animateClock(node);
                        }
                        node.querySelectorAll('.clock-icon-animated').forEach((el) => {
                            console.log('Found nested clock-icon-animated', el);
                            animateClock(el);
                        });
                    }
                });
            }
        });
    });

    // Observe the document body or a specific container
    const target = document.body || document.documentElement;
    observer.observe(target, {
        childList: true,
        subtree: true
    });

    // Initial check for existing .clock-icon-animated elements
    document.querySelectorAll('.clock-icon-animated').forEach((el) => {
        console.log('Initial clock-icon-animated', el);
        animateClock(el);
    });
});