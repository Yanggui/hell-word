//see https://github.com/rackt/react-redux/blob/master/src/components/connect.js
"use strict";
var connect = require('react-redux').connect;

module.exports = function (mapDispatchToProps) {
    return connect(null, mapDispatchToProps);
};