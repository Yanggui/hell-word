"use strict";
var _ = require('lodash');

var appWebtrends = {
    dcsMultiTrack(tracks) {
        var dCur = new Date();
        var dSes = new Date('1970-01-01');
        var divMdcCenter = document.getElementById('mdcCenter');
        tracks['WT.dcsvid'] = divMdcCenter.getAttribute('data-userid');
        tracks['WT.z_dcsid'] = divMdcCenter.getAttribute('data-dcsid');
        tracks['WT.vtvs'] = (dCur.getTime() - dSes.getTime()).toString(); //since 1970.1.1

        var params = [];
        _.keys(tracks).map(key => {
            params.push(key);
            params.push(tracks[key]);
        });

        // check if dcsMultiTrack exists, abandon after retry 100 times.
        var counter = 0;
        var maxCount = 100;
        var sleepyAlert = setInterval(function(){
            if(window.dcsMultiTrack || counter === maxCount){
                clearInterval(sleepyAlert);
                window.dcsMultiTrack.apply(this, params);
            }
            counter += 1;
        }, 200);
    }
};

module.exports = appWebtrends;
