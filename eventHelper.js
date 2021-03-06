"use strict";

var helper = {};
helper.addEventListener = function (target, eventType, callback) {
    if (target.addEventListener) {
        target.addEventListener(eventType, callback, false);
    } else if (target.attachEvent) {
        target.attachEvent('on' + eventType, callback);
    }
};

helper.removeEventListener = function (target, eventType, callback) {
    if (target.removeEventListener) {
        target.removeEventListener(eventType, callback, false);
    } else if (target.detachEvent) {
        target.detachEvent('on' + eventType, callback);
    }
};

helper.noop = function () {
};

helper.stopPropagation = function (e) {
    if (e.stopPropagation) {
        e.stopPropagation();
    }
    else {
        e.cancelBubble = true;
        e.returnValue = false;
    }
    
   if(e.nativeEvent&&e.nativeEvent.stopImmediatePropagation){
       e.nativeEvent.stopImmediatePropagation();
       }
};

helper.preventDefault = function (e) {
    if (e.preventDefault) {
        e.preventDefault();
    }
};

helper.pauseEvent = function (e) {
    helper.stopPropagation(e);
    helper.preventDefault(e);
};

module.exports = helper;