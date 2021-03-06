﻿window.showUpsert = function (id, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.show({ id: id }, urlToGo).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });

};

window.showUpsertParams = function (params, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.show(params, urlToGo).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });

};

window.showConfirmServiceWithMsg = function (params, msg, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.doConfirmMsg(params, msg, urlToGo).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });
};

window.showConfirmService = function (id, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.doConfirm({ id: id }, urlToGo).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });
};


window.showConfirmCancelDocument = function (id, folio, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.doCancelDocument({ uuid: id }, urlToGo, folio).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });
};

window.showObsolete = function (id, divScope, urlToGo, jqGridToUse) {
    var scope = angular.element($(divScope)).scope();
    scope.doObsolete({ id: id }, urlToGo).
        then(function () { $(jqGridToUse).trigger("reloadGrid"); });
};

window.showModalFormDlg = function(divModalid, formId) {
    var dlgCat = $(divModalid);
    dlgCat.modal('show');

    $.validator.unobtrusive.parse(formId);

    $(divModalid).injector().invoke(function ($compile, $rootScope) {
        $compile($(divModalid))($rootScope);
        $rootScope.$apply();
    });

    var scope = angular.element(dlgCat).scope();
    scope.setDlg(dlgCat);
};

window.sendPostAction = function(id, divScope, urlToGo, innerScp, showSuccess) {
    var scope = angular.element($(divScope)).scope();
    scope.sendPostAction({ id: id }, urlToGo, innerScp, showSuccess);
};

window.goToUrlMvcUrl = function (url, params) {
    if (params !== undefined) {
        for (var key in params) {
            var param = params[key] || '';
            url = url.replace(key, param);
        }
    }

    try {
        window.location.replace(url);
    } catch (e) {
        window.location = url;
    }
};

window.initCatalog = function (lstCatalog, catalogId) {
    if (lstCatalog === undefined || lstCatalog === null || lstCatalog.length === 0)
        return undefined;

    if (catalogId === undefined) {
        return lstCatalog[0];
    }

    var catalog;
    for (var i = 0, len = lstCatalog.length; i < len; i++) {
        catalog = lstCatalog[i];
        if (catalog.StKey == catalogId)
            return catalog;
    }

    return lstCatalog[0];
};