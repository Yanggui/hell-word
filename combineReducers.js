//see https://github.com/gajus/redux-immutable/blob/master/src/combineReducers.js
"use strict";
var _ = require('lodash');
var helper = require('../immutableHelper');

var iterator = function (domain, action, reducer, tapper) {
    if (!domain.get) {
        throw new Error('Domain must be an Tree or Cursor.');
    }
    var newDomain = domain;
    var handler = reducer[action.type];
    if (action.domainPaths && action.domainPaths.length > 0) {
        newDomain = domain.select(action.domainPaths);
        handler = _.get(reducer, action.domainPaths.concat(action.type));
    }
    if (_.isFunction(handler)) {
        tapper.isActionHandled = true;
        var args = [newDomain].concat(action.payload);
        handler.apply(this, args);
    }
    return domain;
};

var combineReducers = function (reducer) {
    return function (state, action) {
        if (!action) {
            throw new Error('Action parameter value must be an object.');
        }
        if (action.type && action.type.indexOf('@@') === 0) {
            return state;
        }
        var tapper = {isActionHandled: false};
        var newState = new helper.Tree(state.data);
        newState = iterator(newState, action, reducer, tapper);
        if (!tapper.isActionHandled && action.name !== 'CONSTRUCT') {
            console.warn('Unhandled action "' + action.type + '".', action);
        }
        return newState.data === state.data ? state : newState;
    };
};

module.exports = combineReducers;