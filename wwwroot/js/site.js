// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var globalFileVariable;
var i = 0;
// Set up handlers on load.
window.addEventListener("load", function () {

    // Compute the hash when a selection is made with the file input
    var fileinput = document.getElementById('fileinput');
    fileinput.addEventListener('change', handleFileInputChange);

    var studentSubmissions = document.getElementById('studentSubmissions');
    studentSubmissions.addEventListener('change', handlestudentSubmissionsChange);

    // Compute the hash when a file is dropped into the drop area
    var dropArea = document.getElementById('drop-area');
    dropArea.addEventListener('dragover', handleDropAreaDragover);
    dropArea.addEventListener('drop', handleDropAreaDrop);

    // Open the file input prompt when the drop area is clicked
    //dropArea.addEventListener('click', handleDropAreaClick);								 

});

// Event handlers
/*
function handlestudentSubmissionsChange() {
    var fileinput = this;
    let file = fileinput.files[0];
    $.ajax({
        url: "HomeController/GetFileName",
        type: "POST",
        dataType: "string",
        data: file.name,
        success: function (mydata) {
            alert("something");
        }
    });
}
*/
function handleFileInputChange() {
    // Hash a local file when selected via file input.
    clearResult();
    var fileinput = this;
    let file = fileinput.files;
    globalFileVariable = file;
    hashFile(globalFileVariable[0])
        .then(showHash)
        .catch(showHashError);
}

function handleDropAreaDragover(evt) {
    // Set up the drop area.
    evt.preventDefault();
    evt.dataTransfer.dropEffect = 'copy';
}



function handleDropAreaDrop(evt) {
    // Hash the file that is dropped into the drop area.
    evt.preventDefault();
    clearResult();
    let file = evt.dataTransfer.files;
    globalFileVariable = file;
    for (i = 0; i < globalFileVariable.length; i++) {

        hashFile(globalFileVariable[i])
            .then(showHash)
            .catch(showHashError);
    }
    /*
  hashFile(globalFileVariable)
    .then(showHash)
    .catch(showHashError);
    */
}

function handleDropAreaClick(evt) {
    // Show the file select dialog.
    evt.preventDefault();
    var doc = document.getElementById('fileinput').ownerDocument;
    var fileinputClick = doc.createEvent('MouseEvents');
    fileinputClick.initEvent('click', true, true);
    fileinput.dispatchEvent(fileinputClick, true);
}

// Utilities

function bufferToHex(buffer) {
    // Convert a buffer into a hexadecimal string.
    // From https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
    var hexCodes = [];
    var view = new DataView(buffer);

    for (var i = 0; i < view.byteLength; i += 4) {
        // Using getUint32 reduces the number of iterations needed (we process
        // 4 bytes each time).
        var value = view.getUint32(i);
        // toString(16) will give the hex representation of the number without
        // padding
        var stringValue = value.toString(16);
        // We use concatenation and slice for padding.
        var padding = '00000000';
        var paddedValue = (padding + stringValue).slice(-padding.length);
        hexCodes.push(paddedValue);
    }

    return hexCodes.join("");
}

function bufferToBase64(buffer) {
    // Convert a buffer into a base64 string.
    var str = '';
    var bytes = new Uint8Array(buffer);
    var len = bytes.byteLength;

    for (var i = 0; i < len; i++) {
        str += String.fromCharCode(bytes[i]);
    }

    return window.btoa(str);
}

function hashFile(file) {
    // Hash a File object.
    // Returns a Promise of a successful hash.
    return new Promise(function (resolve, reject) {
        var fileReader = new FileReader();
        fileReader.addEventListener('load', function () {

            crypto.subtle.digest("SHA-256", this.result)
                .then(resolve)
                .catch(reject);
        });
        fileReader.readAsArrayBuffer(file);
    });
}

// DOM mutation

function showHash(hash) {
    // Display the hash as a hex string.
    var filePath = document.getElementById('fileinput');
    var sha256 = bufferToHex(hash);
    console.log(sha256);
    //var filename = globalFileVariable[i].name;
    //document.getElementById('result').innerHTML =
    //'<h2>SHA-256 Hash</h2><input type="text" id="hashValue" value="" style="width:470px;" readonly><button onclick="copyHash()">Copy hash</button>';
    //document.getElementById('hashValue').value = sha256;
    writeHashToTable(sha256);
}

function copyHash() {
    var copyText = document.getElementById("hashValue");

    copyText.select();
    copyText.setSelectionRange(0, 99999); /*For mobile devices*/

    /* Copy the text inside the text field */
    document.execCommand("copy");
}

function clearResult() {
    //document.getElementById('result').innerHTML = '';
    document.getElementById('hashTable').getElementsByTagName('tbody')[0].innerHTML = '';
}

function showHashError(err) {
    // Display a generic hash error.
    document.getElementById('result').innerHTML =
        '<h2>' + gettext('error-header') + '</h2>' +
        '<p>' + gettext('error-hash');
}

function writeHashToTable(hash) {
    var tbodyRef = document.getElementById('hashTable').getElementsByTagName('tbody')[0];
    var newRow = tbodyRef.insertRow();
    var newCell = newRow.insertCell();
    var newText = document.createTextNode(hash);
    newCell.appendChild(newText);
    //var div = hash;
    //document.getElementById("hash").innerHTML = div;
}

function download_table_as_csv(table_id, separator = ',') {
    // Select rows from table_id
    var rows = document.querySelectorAll('table#' + table_id + ' tr');
    // Construct csv
    var csv = [];
    for (var i = 0; i < rows.length; i++) {
        var row = [], cols = rows[i].querySelectorAll('td, th');
        for (var j = 0; j < cols.length; j++) {
            // Clean innertext to remove multiple spaces and jumpline (break csv)
            var data = cols[j].innerText.replace(/(\r\n|\n|\r)/gm, '').replace(/(\s\s)/gm, ' ')
            // Escape double-quote with double-double-quote (see https://stackoverflow.com/questions/17808511/properly-escape-a-double-quote-in-csv)
            data = data.replace(/"/g, '""');
            // Push escaped string
            row.push('"' + data + '"');
        }
        csv.push(row.join(separator));
    }
    var csv_string = csv.join('\n');
    // Download it
    var filename = 'export_' + table_id + '_' + new Date().toLocaleDateString() + '.csv';
    var link = document.createElement('a');
    link.style.display = 'none';
    link.setAttribute('target', '_blank');
    link.setAttribute('href', 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv_string));
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}