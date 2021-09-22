(async function() {

    console.log('initializing log viewer.');

    function onDomReady(callback) {
        if (document.readyState === 'interactive' || document.readyState === 'complete') {
            callback();
        } else {
            document.addEventListener('DOMContentLoaded', callback);
        }
    }

    let tableBody = document.getElementById('tblLogBody');

    onDomReady(() => {
        tableBody = document.getElementById('tblLogBody');
        loadRootRows();
    });
    
    async function loadRootRows() {
        const data = await fetchFileData();
        if (!data) {
            return;
        }
        insertTableRowsAt(0, data.nodes);
    }
    
    async function fetchFileData() {
        const result = await fetch('../file');
        if (result.ok) {
            return await result.json();
        }
    }

    async function fetchSubNodes(nodeId) {
        const result = await fetch(`../node/${nodeId}`);
        if (result.ok) {
            return await result.json();
        }
    }

    function insertTableRowsAt(rowIndex, nodes) {
        for (let i = 0 ; i < nodes.length ; i++) {
            insertOneTableRowAt(rowIndex + i, nodes[i]);
        }
    }

    function insertOneTableRowAt(rowIndex, node) {
        const tr = tableBody.insertRow(rowIndex);
        const tds = [
            tr.insertCell(-1),
            tr.insertCell(-1),
            tr.insertCell(-1),
            tr.insertCell(-1)
        ];

        const indentPx = (node.depth + (node.hasNodes ? 0 : 1)) * 30;
        const indentSpan = document.createElement('span');
        indentSpan.className = 'log-td-indent'
        indentSpan.style.width = `${indentPx}px`;
        tds[0].appendChild(indentSpan);
        
        if (node.hasNodes) {
            const expandAnchor = document.createElement('a');
            expandAnchor.className = 'log-td-expand';
            expandAnchor.onclick = () => expandNode(node);
            expandAnchor.innerText = '[+]';
            tds[0].appendChild(expandAnchor);
        }

        const messageSpan = document.createElement('span');
        messageSpan.className = 'log-td-msg';
        messageSpan.innerText = node.message;
        tds[0].appendChild(messageSpan);

        tds[1].className = 'col-time';
        tds[1].innerText = node.time.substr(8, 15);

        tds[2].className = 'col-duration';
        tds[2].innerText = node.duration >= 0 ? parseInt(node.duration * 1000) : ''
    }
    
    async function expandNode(node) {
        var data = await fetchSubNodes(node.id);
        if (!data) {
            return;
        }
    }

})();

