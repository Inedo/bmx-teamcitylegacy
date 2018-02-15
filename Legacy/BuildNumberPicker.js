
function BmTeamCityBuildNumberPicker(o) {
    /// <param name="o" value="{
    /// hiddenFieldSelector: '',
    /// buildConfigSelector: '',
    /// buildConfigId: '',
    /// configurerId: ''
    /// includeBuildNumbers: ''
    /// }"/>

    var createSearchChoice = function (term) {
        return {
            id: encodeURIComponent(term),
            text: term
        };
    };

    var loadItems = function (buildConfigId) {
        $.ajax({
            url: o.ajaxUrl,
            type: 'POST',
            data: { buildConfigurationId: buildConfigId, configurerId: o.configurerId, includeBuildNumbers: o.includeBuildNumbers },
            success: function (data) {
                $(o.hiddenFieldSelector).select2({
                    createSearchChoice: createSearchChoice,
                    allowCreate: true,
                    data: data
                });
            }
        });
    };

    if (o.buildConfigId) {
        loadItems(o.buildConfigId);
    }
    else {
        $(o.buildConfigSelector).change(function () {
            loadItems($(o.buildConfigSelector).val());            
        }).change();
    }
}