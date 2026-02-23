/**
 * @NApiVersion 2.x
 * @NScriptType Restlet
 */

define(['N/search'], function (search) {

    function get(context) {

        var pageSize = parseInt(context.pageSize, 10) || 1000;
        var pageIndex = parseInt(context.pageIndex, 10) || 0;
        var lastSyncDate = context.lastSyncDate;

        // -------------------------
        // Build Filters
        // -------------------------
        var filters = [
            ["isinactive", "is", "F"],
            "AND",
            ["custitem_headless_commerce_product", "is", "T"]
        ];

        if (lastSyncDate) {
            filters.push("AND");
            filters.push(["modified", "onorafter", lastSyncDate]);
        }

        // -------------------------
        // Create Search
        // -------------------------
        var itemSearch = search.create({
            type: "item",
            filters: filters,
            columns: [
                search.createColumn({
                    name: "internalid",
                    summary: search.Summary.GROUP
                }),
                search.createColumn({
                    name: "itemid",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "displayname",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "salesdescription",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "baseprice",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "thumbnailurl",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "quantityavailable",
                    summary: search.Summary.MAX
                }),
                search.createColumn({
                    name: "formulatext",
                    summary: search.Summary.MAX,
                    formula: "NS_CONCAT({commercecategoryid})"
                }),
                search.createColumn({
                    name: "modified",
                    summary: search.Summary.MAX
                })
            ]
        });

        var pagedResults = itemSearch.runPaged({
            pageSize: pageSize
        });

        var items = [];

        // -------------------------
        // Fetch Page
        // -------------------------
        if (pagedResults.pageRanges.length > pageIndex) {

            var page = pagedResults.fetch({
                index: pageIndex
            });

            page.data.forEach(function (result) {

                var rawCategories = result.getValue({
                    name: "formulatext",
                    summary: search.Summary.MAX
                });

                var categoryArray = [];

                if (rawCategories) {

                    var splitCategories = rawCategories.split(',');

                    for (var i = 0; i < splitCategories.length; i++) {
                        var trimmed = splitCategories[i].trim();

                        if (trimmed !== "") {
                            categoryArray.push(trimmed);
                        }
                    }

                    // Remove duplicates (ES5 compatible)
                    var uniqueMap = {};
                    var uniqueList = [];

                    for (var j = 0; j < categoryArray.length; j++) {
                        var val = categoryArray[j];

                        if (!uniqueMap[val]) {
                            uniqueMap[val] = true;
                            uniqueList.push(val);
                        }
                    }

                    categoryArray = uniqueList;
                }

                items.push({
                    id: result.getValue({
                        name: "internalid",
                        summary: search.Summary.GROUP
                    }),
                    sku: result.getValue({
                        name: "itemid",
                        summary: search.Summary.MAX
                    }),
                    name: result.getValue({
                        name: "displayname",
                        summary: search.Summary.MAX
                    }),
                    description: result.getValue({
                        name: "salesdescription",
                        summary: search.Summary.MAX
                    }) || "",
                    price: parseFloat(result.getValue({
                        name: "baseprice",
                        summary: search.Summary.MAX
                    })) || 0,
                    quantityAvailable: parseInt(result.getValue({
                        name: "quantityavailable",
                        summary: search.Summary.MAX
                    }), 10) || 0,
                    imageUrl: result.getValue({
                        name: "thumbnailurl",
                        summary: search.Summary.MAX
                    }) || "",
                    lastModifiedDate: result.getValue({
                        name: "modified",
                        summary: search.Summary.MAX
                    }),
                    categoryIds: categoryArray
                });
            });
        }

        // -------------------------
        // Response
        // -------------------------
        return JSON.stringify({
            success: true,
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