"use strict";
var update = require('react-addons-update');
var _ = require('lodash');

var helper = {
    get: function (data, paths) {
        var last = data;
        paths = paths || [];
        paths = helper.getPaths(data, paths);
        paths.forEach(function (path) {
            last = last[path];
        });
        return last;
    },
    getPaths: function (data, paths) {
        var finalPaths = [];
        paths.forEach(function (path) {
            path = typeof path === 'object' ? _.findIndex(helper.get(data, finalPaths), path) : path;
            finalPaths.push(path);
        });
        return finalPaths;
    },
    operate: function (data, paths, op, target) {
        var finalPaths = helper.getPaths(data, paths);
        var obj = {};
        var last = obj;
        for (var i = 0; i < finalPaths.length - 1; i++) {
            last[finalPaths[i]] = {};
            last = last[finalPaths[i]];
        }

        if (op === 'set') {
            last[finalPaths[i]] = {$set: target};
        }
        if (op === 'merge') {
            last[finalPaths[i]] = {$merge: target};
        }
        if (op === 'push') {
            last[finalPaths[i]] = {$push: [].concat(target)};
        }
        if (op === 'unshift') {
            last[finalPaths[i]] = {$unshift: [].concat(target)};
        }
        if (op === 'splice') {
            var args = Array.prototype.slice.call(arguments, 3);
            last[finalPaths[i]] = {$splice: [args]};
        }
        return update(data, obj);
    },
    delete: function (data, paths) {
        //if index is object, find index of it;
        var newPaths = paths.slice(0, paths.length - 1);
        var index = paths.slice(paths.length - 1)[0];
        index = typeof index === 'object' ? _.findIndex(helper.get(data, newPaths), index) : index;
        if (index !== -1) {
            return helper.operate(data, newPaths, 'splice', index, 1);
        }
        return data;
    }
};
['set', 'merge', 'push', 'unshift', 'splice'].forEach(function (op) {
    helper[op] = function (data, paths) {
        var args = [this.data, paths, op].concat(Array.prototype.slice.call(arguments, 2));
        return helper.operate.apply(this, args);
    };
});

var ops = ['get', 'set', 'merge', 'push', 'unshift', 'splice', 'delete'];
(function () {
    var Tree = function (data) {
        this.data = data;
    };
    ops.forEach(function (op) {
        Tree.prototype[op] = function (paths) {
            var args = [this.data, paths].concat(Array.prototype.slice.call(arguments, 1));
            if (op === 'get') {
                return helper[op].apply(this, args);
            }
            else {
                this.data = helper[op].apply(this, args);
                return this.data;
            }
        };
    });
    Tree.prototype.select = function (paths) {
        return new helper.Cursor(this, paths);
    };

    helper.Tree = Tree;
})();

(function () {
    var Cursor = function (tree, paths) {
        this.tree = tree;
        this.paths = helper.getPaths(tree.data, paths);
    };
    ops.forEach(function (op) {
        Cursor.prototype[op] = function (paths) {
            var args = [this.paths.concat(paths || [])].concat(Array.prototype.slice.call(arguments, 1));
            return helper.Tree.prototype[op].apply(this.tree, args);
        };
    });
    Cursor.prototype.select = function (paths) {
        return new helper.Cursor(this.tree, this.paths.concat(paths || []));
    };
    Cursor.prototype.emitChange = function () {
        this.tree.emitChange();
    };

    helper.Cursor = Cursor;
})();

module.exports = helper;