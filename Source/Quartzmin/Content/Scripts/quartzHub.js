"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("quartzHub").build();

connection.on("Update", function (updateInfo) {
    //console.log(updateInfo);
    $('#executedJobs').text(updateInfo.executedJobs);
    $('#failedJobs').text(updateInfo.failedJobs);
    $('#executingJobs').text(updateInfo.executingJobs);
    $('#jobsCount').text(updateInfo.jobsCount);
    $('#triggerCount').text(updateInfo.triggerCount);
});

connection.start().then(function () {
    // something to do after started
}).catch(function (err) {
    return console.error(err.toString());
});

setInterval(function() {
    connection.invoke("GetScheduleInfoAsync").catch(function (err) {
        return console.error(err.toString());
    });
}, 10000);