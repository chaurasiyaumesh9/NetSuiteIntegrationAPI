/**
 * @NApiVersion 2.x
 * @NScriptType Restlet
 */

define(['N/search'], function (search) {

    function get(context) {

        var categoryId = context.categoryId;
        var pageSize = parseInt(context.pageSize) || 1000;
        var pageIndex = parseInt(context.pageIndex) || 0;

        if (!categoryId) {
            return {
                success: false,
                message: "categoryId is required"
            };
        }

        var itemSearch = search.create({
            type: search.Type.ITEM,
            filters: [
                ['isinactive', 'is', 'F'],
                'AND',
                ['commercecategoryid', 'anyof', categoryId]
            ],
            columns: [
                'internalid',
                'itemid',
                'displayname',
                'salesdescription',
                'baseprice',
                'storedisplaythumbnail',
                'quantityavailable'
            ]
        });

        var pagedResults = itemSearch.runPaged({
            pageSize: pageSize
        });

        var items = [];

        if (pagedResults.pageRanges.length > pageIndex) {

            var page = pagedResults.fetch({
                index: pageIndex
            });

            page.data.forEach(function (result) {
                items.push({
                    id: result.getValue('internalid'),
                    sku: result.getValue('itemid'),
                    name: result.getValue('displayname'),
                    description: result.getValue('salesdescription'),
                    price: result.getValue('baseprice'),
                    imageUrl: result.getValue('storedisplaythumbnail'),
                    quantityAvailable: result.getValue('quantityavailable')
                });
            });
        }

        return JSON.stringify({
            success: true,
            categoryId: categoryId,
            totalResults: pagedResults.count,
            pageIndex: pageIndex,
            pageSize: pageSize,
            items: items
        });
    }

    return {
        get: get
    };
});