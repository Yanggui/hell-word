"use strict";
var assign = require('object-assign');
var EventEmitter = require('events').EventEmitter;
var P = require('bluebird');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var utils = require('./');

var createStore = function (store) {
    var CHANGE_EVENT = 'change';
    var handlers = {};

    assign(store, EventEmitter.prototype, {
        emitChange: function () {
            this.emit(CHANGE_EVENT);
        },
        addChangeListener: function (callback) {
            this.on(CHANGE_EVENT, callback);
        },
        removeChangeListener: function (callback) {
            this.removeListener(CHANGE_EVENT, callback);
        }
    });

    assign(store, utils.Tree.prototype);

    store.register = function (handler, actionType) {
        if (handlers[actionType]) {
            throw 'action ' + actionType + ' already exists';
        }
        //console.log(actionType);
        handlers[actionType] = handler;
        return actionType;
    };

    store.dispatchToken = AppDispatcher.register(function (action) {
        var handler = handlers[action.type];
        if (handler) {
            var changed = handler.apply(this, action.arguments);
            if (changed !== false) {
                store.emitChange();
            }
        }
        //if (process.env.NODE_ENV === "development") {
        //    console.log({actionType: action.type, args: action.arguments, state: store.getData()});
        //}
    });

    return store;
};

//see https://gist.github.com/gaearon/886641422b06a779a328
//    https://github.com/rackt/react-router/issues/700
var observeStore = function (store, predicate) {
    var performCheck;

    return new P(resolve => {
        performCheck = () => {
            if (predicate.call(null, store)) {
                resolve();
            }
        };

        store.addChangeListener(performCheck);
        performCheck();
    }).finally(() => {
            store.removeChangeListener(performCheck);
        });
};

module.exports = {
    createStore: createStore,
    observeStore: observeStore
};