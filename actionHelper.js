"use strict";
var AppDispatcher = require('../dispatcher/AppDispatcher');

var createViewAction = function (actionType, source, tree) {
    return function () {
        AppDispatcher.dispatch({
            source: source,
            type: actionType,
            arguments: [tree].concat(Array.prototype.slice.call(arguments, 0))
        });
    };
};

var Actions = function (defaultStore, name) {
    this.name = name;
    this.store = defaultStore;
};

var createAction = function (handler, actionName, source) {
    var actionType = this.store.register(handler, this.name + '.' + actionName);
    return createViewAction(actionType, source, this.store);
};

Actions.prototype.createAction = function (handler, actionName) {
    return createAction.call(this, handler, actionName, 'VIEW_ACTION');
};

Actions.prototype.createServerAction = function (handler, actionName) {
    return createAction.call(this, handler, actionName, 'SERVER_ACTION');
};

var createActions = function (defaultStore, name) {
    var actions = new Actions(defaultStore, name);
    return actions;
};

module.exports = {
    createActions: createActions
};