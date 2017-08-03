"use strict";
var _ = require('lodash');

var formatNum = function (num, fixedCount) {
    if (fixedCount === 0) {
        return Math.round(+num);
    }
    return (+num).toFixed(fixedCount).replace(/\.?0+$/, '');
};

var format = function (val, formatType) {
    if (val === 'NA' || val === '' || val === '-' || val === '--' || (_.isString(val) && val.indexOf('%') > -1)) {
        if(val===''){
            return '--';
        }
        return val.toUpperCase();
    }
    var formatNum = function (num, fixedCount) {
        if (fixedCount === 0) {
            return Math.round(+num);
        }
        return (+num).toFixed(fixedCount).replace(/\.?0+$/, '');
    };
    if (formatType === 'Monetary') {
        return formatNum(val, 2);
    }
    if (formatType === 'Percentage') {
        var numStr = formatNum(val * 100, 2);
        return numStr === 'NaN' ? '--' : numStr + '%';
    }
    if (formatType === 'Integer') {
        return Math.floor(val * 100) / 100;
    }
    if (formatType === 'JsonDate'){
        var date = val.slice(6,val.length-2);
        //date*1 make it to number
        return new Date(date*1);
    }
    if(formatType === 'Date'){
        var month_abbrs = [
            'Jan',
            'Feb',
            'Mar',
            'Apr',
            'May',
            'Jun',
            'Jul',
            'Aug',
            'Sep',
            'Oct',
            'Nov',
            'Dec'
        ];
        var formatedStr;
            formatedStr = val.getUTCDate();
            formatedStr += ' ' + month_abbrs[val.getUTCMonth()];
            formatedStr += ' ' + val.getUTCFullYear();
        return formatedStr;
    }
    if(formatType === 'BigNumber'){
        if(!isNaN(val)){
            return val.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        }
    }
    return val;
};

var Formatter = {
    Percentage: function (text) {
        text = (text + '').replace(/[^\d\.\+-]/g, '').replace(/\.$/, '');
        text = (text === '--') ? '' : text;
        if(isNaN(text)){
            return text;
        }else{
            return text + '%';
        }
    },

    Monetary: function (text) {
        text = (text + '').replace(/[^\d\.\+-]/g, '').replace(/\.$/, '');
        text = (text === '--') ? '' : text;
        if(isNaN(text)){
            return text;
        }else{
            return '$' + text;
        }
    },

    Integer: function (text) {
        text = (text + '').replace(/[^\d\.\+-]/g, '').replace(/\.0*$/, '');
        text = (text === '--') ? '' : text;
        return text;
    }
};

var Validator = {
    Percentage: function (text) {
        var val = parseFloat(text);
        return !isNaN(val);
    },

    Monetary: function (text) {
        text = (text + '').replace(/^\$/, '');
        var val = parseFloat(text);
        return !isNaN(val);
    },

    Integer: function (text) {
        var val = parseFloat(text);
        return !isNaN(val);
    }
};

module.exports = {
    formatNum: formatNum,
    format: format,
    Formatter: Formatter,
    Validator: Validator
};