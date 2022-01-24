module.exports =  (callback) => {
    var stringToBuffer = function (t) {
        for (var e = new ArrayBuffer(t.length), n = new Uint8Array(e), r = 0; r < t.length; r++)
            n[r] = t.charCodeAt(r);
        return e
    };
    var bufferToString = function (array) {
        for (var e = new Uint8Array(array), n = "", r = 0; r < e.byteLength; r++)
            n += String.fromCharCode(e[r]);
        return btoa(n).replace(/=/g, "").replace(/\+/g, "-").replace(/\//g, "_")
    };
    
    var value = '{"loginAvailable":2,"newPassword":[180,2557],"confirmPassword":0,"question":-1,"answer":0,"orderType":"captcha","lastChangeToSubmit":0,"string":"ae42de4f5f0b6dd3ba5e","number":[83,99],"ctime":"' + parseInt(new Date().getTime() / 1000) + '}';

    let randomValues = crypto.getRandomValues(new Uint8Array(12));
    crypto.subtle.generateKey({name: "AES-GCM", length: 256}, !0, ["encrypt", "decrypt"]).then(result => {
        var key = result;
        let algo = {iv: randomValues, name: 'AES-GCM', tagLength: 128};
        let wrapAlgo = {name: "RSA-OAEP", hash: {name: "SHA-256"}};
        crypto.subtle.encrypt(algo, key, stringToBuffer(value)).then(result =>{
            const encryptedString = bufferToString(result);
            const ivString = bufferToString(randomValues);
            const token = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA547nGY8zjkKObfqzcuUcqEFRmn5ZlUGGGTAum9ihZYy60StWz/HtgQaX77I1K22qcB0h8L/VSp7syv0Q1Rvit9kZ2G9zk1P97jVYixeL9deVwMkofnvzy6u4N0rjhpZkeQyGa2JOFW5b1Rk1Jk3ShV74V+LRdwhVIsR69O+POP7mH3QMB7Ei5as0Dzh2tAQBJ6CyMjyiC3HztUilHMviQFWUGlXbyKfCCWDhmwCiiTHR9T33d3hvvZw+9IxZXVSTy2cOan6rI7UkcVAs/VZBvTurBrqivCv6gYfoPziAOQCViEa+cBk4JLPwzPsbOrqlmnsix0Io7toFJO8Or9wtjwIDAQAB";
            crypto.subtle.importKey('spki', stringToBuffer(atob(token)), wrapAlgo, !1, ["wrapKey"])
                .then(result => {
                    crypto.subtle.wrapKey('raw', key, result, wrapAlgo).then(result =>{
                        let secondPart = bufferToString(result);
                        let resultS = encryptedString + '.' + secondPart + '.' + ivString;
                        callback(null, resultS);
                    });
                })
        });
    });
}