//see https://github.com/acdlite/redux-actions
"use strict";
var _ = require('lodash');

var createAction = function (type, source, domainPaths) {
    return (...args) => {
        const action = {
            type,
            source: source,
            domainPaths: domainPaths,
            payload: [...args]
        };

        if (args.length === 1 && args[0] instanceof Error) {
            // Handle FSA errors where the payload is an Error object. Set error.
            action.error = true;
        }

        return action;
    };
};

var iterate = function (actions, domainPaths, reducer) {
    _.forEach(reducer, function(value, key) {
        if (_.isFunction(value)) {
            actions[key] = createAction.call(actions, key, '', domainPaths);
        }
        else if (_.isPlainObject(value)) {
            iterate(actions, domainPaths.concat(key), value);
        }
    });
};

//createActions
var createActions = (function () {
    var Actions = function (domainPaths) {
        this.domainPaths = domainPaths || [];
    };

    Actions.prototype.createActionByReducer = function (reducer) {
        var domainPaths = reducer.__domainPath__ || [];
        var actions = this;
        iterate(actions, domainPaths, reducer);
    };

    Actions.prototype.createAction = function (temp, type, paths) {
        return createAction.call(this, type, 'VIEW_ACTION', this.domainPaths.concat(paths || []));
    };

    Actions.prototype.createServerAction = function (temp, type, paths) {
        return createAction.call(this, type, 'SERVER_ACTION', this.domainPaths.concat(paths || []));
    };

    var createActions = function (domainPaths) {
        return new Actions(domainPaths);
    };
    return createActions;
})();

module.exports = createActions;