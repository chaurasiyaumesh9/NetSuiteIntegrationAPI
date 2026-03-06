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

                search.createColumn({ name: "internalid", summary: search.Summary.GROUP }),
                search.createColumn({ name: "itemid", summary: search.Summary.MAX }),
                search.createColumn({ name: "displayname", summary: search.Summary.MAX }),
                search.createColumn({ name: "salesdescription", summary: search.Summary.MAX }),
                search.createColumn({ name: "baseprice", summary: search.Summary.MAX }),
                search.createColumn({ name: "thumbnailurl", summary: search.Summary.MAX }),
                search.createColumn({ name: "quantityavailable", summary: search.Summary.MAX }),
                search.createColumn({
                    name: "formulatext",
                    summary: search.Summary.MAX,
                    formula: "NS_CONCAT({commercecategoryid})"
                }),
                search.createColumn({ name: "modified", summary: search.Summary.MAX }),

                // 🔥 NEW FIELDS
                search.createColumn({ name: "custitem_brand", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_color_headless", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_size_headless", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_material_headless", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_style_headless", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_gender_headless", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_featured_item", summary: search.Summary.MAX }),
                search.createColumn({ name: "custitem_customer_rating", summary: search.Summary.MAX })
            ]
        });

        var pagedResults = itemSearch.runPaged({ pageSize: pageSize });

        var items = [];

        // -------------------------
        // Fetch Page
        // -------------------------
        if (pagedResults.pageRanges.length > pageIndex) {

            var page = pagedResults.fetch({ index: pageIndex });

            page.data.forEach(function (result) {

                // -------------------------
                // Category Handling
                // -------------------------
                var rawCategories = result.getValue({
                    name: "formulatext",
                    summary: search.Summary.MAX
                });

                var categoryArray = [];

                if (rawCategories) {
                    var splitCategories = rawCategories.split(',');
                    var uniqueMap = {};

                    for (var i = 0; i < splitCategories.length; i++) {
                        var trimmed = splitCategories[i].trim();

                        if (trimmed !== "" && !uniqueMap[trimmed]) {
                            uniqueMap[trimmed] = true;
                            categoryArray.push(trimmed);
                        }
                    }
                }

                // -------------------------
                // Build Item
                // -------------------------
                items.push({
                    id: result.getValue({ name: "internalid", summary: search.Summary.GROUP }),
                    sku: result.getValue({ name: "itemid", summary: search.Summary.MAX }),
                    name: result.getValue({ name: "displayname", summary: search.Summary.MAX }),
                    description: result.getValue({ name: "salesdescription", summary: search.Summary.MAX }) || "",
                    price: parseFloat(result.getValue({ name: "baseprice", summary: search.Summary.MAX })) || 0,
                    quantityAvailable: parseInt(result.getValue({ name: "quantityavailable", summary: search.Summary.MAX }), 10) || 0,
                    imageUrl: result.getValue({ name: "thumbnailurl", summary: search.Summary.MAX }) || "",
                    lastModifiedDate: result.getValue({ name: "modified", summary: search.Summary.MAX }),
                    categoryIds: categoryArray,

                    // 🔥 NEW PROPERTIES
                    brand: result.getValue({ name: "custitem_brand", summary: search.Summary.MAX }) || "",
                    color: result.getValue({ name: "custitem_color_headless", summary: search.Summary.MAX }) || "",
                    size: result.getValue({ name: "custitem_size_headless", summary: search.Summary.MAX }) || "",
                    material: result.getValue({ name: "custitem_material_headless", summary: search.Summary.MAX }) || "",
                    style: result.getValue({ name: "custitem_style_headless", summary: search.Summary.MAX }) || "",
                    gender: result.getValue({ name: "custitem_gender_headless", summary: search.Summary.MAX }) || "",
                    featured: result.getValue({ name: "custitem_featured_item", summary: search.Summary.MAX }),
                    customerRating: parseFloat(result.getValue({ name: "custitem_customer_rating", summary: search.Summary.MAX })) || 0
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