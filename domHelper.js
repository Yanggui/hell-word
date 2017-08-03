"use strict";

var helper = {};
helper.isChildNodeOf = function (target, parentId) {
    while (target && target.id !== parentId) {
        target = target.parentNode;
    }
    return (target && target.id === parentId);
};

helper.updateOverlayPosition = function (target, overlay, overlayContainer, place) {
    var targetTop = target.getBoundingClientRect().top;
    var targetLeft = target.getBoundingClientRect().left;
    var targetWidth = target.clientWidth;
    var targetHeight = target.clientHeight;
    var tipWidth = overlay.clientWidth;
    var tipHeight = overlay.clientHeight;
    var x, y;
    if (place === "top") {
        x = targetLeft - (tipWidth / 2) + (targetWidth / 2);
        y = targetTop - tipHeight;
    }
    else if (place === "bottom") {
        x = targetLeft - (tipWidth / 2) + (targetWidth / 2);
        y = targetTop + targetHeight;
    }
    else if (place === "left") {
        x = targetLeft - tipWidth;
        y = targetTop + (targetHeight / 2) - (tipHeight / 2);
    }
    else if (place === "right") {
        x = targetLeft + tipWidth;
        y = targetTop + (targetHeight / 2) - (tipHeight / 2);
    }
    overlayContainer.style.left = x + 'px';
    overlayContainer.style.top = y + 'px';
    overlayContainer.style.position = 'absolute';
};

helper.scrollIntoView = function (target, parent) {
    if (!target || !parent) {
        return;
    }
    var focusedRect = target.getBoundingClientRect();
    var parentRect = parent.getBoundingClientRect();
    if (focusedRect.bottom > parentRect.bottom || focusedRect.top < parentRect.top) {
        parent.scrollTop = (target.offsetTop + target.clientHeight - parent.offsetHeight);
    }
};

module.exports = helper;