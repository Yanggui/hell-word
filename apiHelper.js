var Qs = require('qs');
require('es6-promise').polyfill();
if (typeof process !== 'object' || process.browser) {
    require('whatwg-fetch');
}

var apiHelper = {
    handleResponse: function (response) {
        if (response.type === 'basic' && response.url) {
            if (response.url.indexOf('/error.aspx') > -1) {
                console.log('webapi error, ' + response.url);
                return '';
            }
            if (response.url.indexOf('/login.aspx') > -1) {
                window.location.reload();
            }
        }
        if (response.status >= 200 && response.status < 300) {
            if (response.headers.get("content-type") &&
                response.headers.get("content-type").toLowerCase().indexOf("application/json") >= 0) {
                return response.json();
            } else {
                return response.text();
            }
        } else {
            if (response.statusText.indexOf('Your access token is invalid') !== -1) {
                window.location.reload();
                return Promise.reject(response);
            }

            var error = new Error(response.statusText);
            error.response = response;
            console.log(error);
        }
    },

    get: function (url) {
        return fetch(url, {credentials: 'include'})
            .then(apiHelper.handleResponse);
    },

    post: function (url, data) {
        return fetch(url,
            {
                method: 'post',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: Qs.stringify(data)
            })
            .then(apiHelper.handleResponse);
    }
};

module.exports = apiHelper;
