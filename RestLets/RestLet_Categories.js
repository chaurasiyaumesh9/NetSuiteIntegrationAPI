/**
 * @NApiVersion 2.x
 * @NScriptType Restlet
 */

define(['N/search'], function (search) {

    function get(context) {

        var categorySearch = search.create({
            type: 'commercecategory',
            filters: [
                ['isinactive', 'is', 'F'],
                'AND',
                ['displayinsite', 'is', 'T'],
                'AND',
                ['custrecord_headless_commerce_category', 'is', 'T']
            ],
            columns: [
                'internalid',
                'name',
                'primaryparent',
                'urlfragment',
                'custrecord_featured_category',
                'thumbnail'
            ]
        });

        var categories = [];

        categorySearch.run().each(function (result) {

            categories.push({
                id: result.getValue('internalid'),
                name: result.getValue('name'),
                primaryParent: result.getValue('primaryparent'),
                urlFragment: result.getValue('urlfragment'),
                featured: result.getValue('custrecord_featured_category'),
                thumbnail: result.getValue('thumbnail')
            });

            return true;
        });

        return JSON.stringify({
            success: true,
            count: categories.length,
            items: categories
        });
    }

    return {
        get: get
    };
});