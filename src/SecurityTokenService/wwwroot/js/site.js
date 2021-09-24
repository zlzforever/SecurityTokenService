// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


$(document).ready(() => {
    $.ajaxSetup({
        complete: function (event, xhr) {
            if (xhr.status === 301 || xhr.status === 302) {
                const url = xhr.getResponseHeader("Location");
                if (url) {
                    let win = window;
                    while (win !== win.top) {
                        win = win.top;
                    }
                    win.location.href = url;
                }
            }
        }
    });

    const url = location.href;
    if (url.indexOf("login.html") >= 0) {
        initLogin();
    } else if (url.indexOf("logout.html") >= 0) {
        initLogout();
    } else if (url.indexOf("loggedout.html") >= 0) {
        initLoggedOut();
    } else if (url.indexOf("diagnostics.html") >= 0) {
        initSession();
        initDiagnostics();
    } else if (url.indexOf("consent.html") >= 0) {
        initSession();
        initConsent();
    } else if (url.indexOf("redirect.html") >= 0) {
        initRedirect();
    } else {
        initSession();
    }
})

function initRedirect() {
    // todo: 是否可以直接跳转？
    const url = getQueryValue('redirectUrl')
    const meta = $('#meta');
    meta.attr('content', '0;url=' + url)
    meta.attr('data-url', url)
    window.location.href = url;
}

function initConsent() {
    const returnUrl = getQueryValue('returnUrl');
    $.ajax({
        xhrFields: {withCredentials: true},
        type: "GET",
        url: "consent?returnUrl=" + returnUrl,
        success: function (res2) {
            const res = res2.data;
            $('#clientName').prepend(res.clientName)
            if (res.clientLogoUrl) {
                $('#clientLogoUrl').attr('src', res.clientLogoUrl)
            } else {
                $('#clientLogoUrl').remove()
            }

            $('#returnUrl').val(res.returnUrl);
            debugger
            if (res.identityScopes && res.identityScopes.length > 0) {
                for (let i = 0; i < res.identityScopes.length; ++i) {
                    const scope = res.identityScopes[i];
                    appendScope('identityScopeList', scope);
                }

                $('#identityScopes').show();
            }
            if (res.resourceScopes && res.resourceScopes.length > 0) {
                for (let i = 0; i < res.resourceScopes.length; ++i) {
                    const scope = res.resourceScopes[i];
                    appendScope('resourceScopes', scope);
                }
                $('#resourceScopes').show();
            }
            if (res.allowRememberConsent) {
                $('#rememberConsentDiv').show();
            } else {
                $('#rememberConsent').val(false);
            }
            if (res.clientUrl) {
                $('#client').attr('href', res.clientUrl)
                $('#clientName2').text(res.clientName)
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {

        }
    });
}

function appendScope(id, scope) {
    const html = `<li class="list-group-item">
<label>
 <input class="consent-scopecheck" type="checkbox"
    name="ScopesConsented"
    id="scopes_${scope.name}"
    value="${scope.name}"
    checked="${scope.checked}"
    disabled="${scope.required}" />
    <strong>${scope.displayName}</strong>
</label>
${scope.required ? "<span><em>(required)</em></span>" : ""}
${scope.description ? '<div class="consent-description"><label for="scopes_' + scope.name + '">' + scope.description + '</label></div>' : ""}
</li>`;
    $('#' + id).append(html);
}

function initDiagnostics() {
    $.ajax({
        xhrFields: {withCredentials: true},
        type: "GET",
        url: "diagnostics",
        success: function (res) {
            debugger

            if (res.claims && res.claims.length > 0) {
                const claimsEl = $("#claims");
                for (let i = 0; i < res.claims.length; ++i) {
                    const claim = res.claims[i];
                    claimsEl.append(`<dt>${claim.type}</dt>`);
                    claimsEl.append(`<dd>${claim.value}</dd>`);
                }
            }
            if (res.properties && res.properties.length > 0) {
                const propertiesEl = $("#properties");
                for (let i = 0; i < res.properties.length; ++i) {
                    const property = res.properties[i];
                    propertiesEl.append(`<dt>${property.key}</dt>`);
                    propertiesEl.append(`<dd>${property.value}</dd>`);
                }
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            if (XMLHttpRequest.status === 401) {
                window.location.href = 'login.html?returnUrl=/diagnostics.html'
            }
        }
    });
}

function initLoggedOut() {
    debugger
    const postLogoutRedirectUri = getQueryValue('postLogoutRedirectUri');
    const clientName = getQueryValue('clientName');
    const signOutIframeUrl = getQueryValue('signOutIframeUrl');
    const automaticRedirectAfterSignOut = getQueryValue('automaticRedirectAfterSignOut');
    if (postLogoutRedirectUri) {
        $('#clientName').text(clientName);
        $('#postLogoutRedirect').show();
        if (automaticRedirectAfterSignOut) {
            window.location = postLogoutRedirectUri;
        }
    }
    if (signOutIframeUrl) {
        $('#signOutIframe').show();
        $('#signOutIframeUrl').attr('src', signOutIframeUrl);
    }
}

function initLogin() {
    $.validator.setDefaults({
        errorContainer: "div.error",
        errorLabelContainer: $("#form div.error"),
        wrapper: "label"
    });

    $('#button').click(function () {
        const message = $('#message');
        message.hide();
        $("#form").validate({
            rules: {
                username: {
                    required: true
                },
                password: {
                    required: true
                }
            },
            messages: {
                username: {
                    required: "用户名不能为空",
                },
                password: {
                    required: "请输入密码"
                }
            },
            submitHandler: function (form) {
                $.ajax({
                    xhrFields: {withCredentials: true},
                    type: "POST",
                    url: "account/login",
                    data: $(form).serialize() + "&returnUrl=" + getQueryValue("returnUrl"),
                    beforeSend: function () {
                        // todo:
                        // $("#loading").css("display", "block"); //点击登录后显示loading，隐藏输入框
                        // $("#login").css("display", "none");
                    },
                    success: function (res) {
                        if (res.code === 301 || res.code === 302) {
                            const url = res.data;
                            if (url) {
                                let win = window;
                                while (win !== win.top) {
                                    win = win.top;
                                }
                                console.log("redirect: " + url)
                                win.location.href = url;
                            }
                        } else if (res.code !== 200) {
                            message.show();
                            switch (res.code) {
                                case 4001: {
                                    message.text('不支持双因素认证')
                                    break;
                                }
                                case 4002: {
                                    message.text('用户被禁止登录')
                                    break;
                                }
                                case 4003: {
                                    message.text('用户被锁定')
                                    break;
                                }
                                case 4004: {
                                    message.text('用户名或密码不正确')
                                    break;
                                }
                                case 4005: {
                                    message.text('不支持 NativeClient')
                                    break;
                                }
                                case 4006: {
                                    message.text('返回地址不合法')
                                    break;
                                }
                            }
                        } else {
                            window.location.href = "/";
                        }
                    },
                    error: function () {
                        console.log('error')
                    }
                });
            },
        });
    });
}

function initLogout() {
    const logoutId = getQueryValue('logoutId');
    debugger
    $("#logoutId").val(logoutId);
}

function initSession() {
    $.get("session", (res) => {
        if (res.data && res.data.sub) {
            const user = $("#user");
            user.attr("sub", res.data.sub);
            user.prepend(res.data.name);
            $("#userNav").show();
        }
    });
}

function getQueryValue(queryName) {
    const query = decodeURI(window.location.search.substring(1));
    const vars = query.split("&");
    for (let i = 0; i < vars.length; i++) {
        const pair = vars[i].split("=");
        if (pair[0] === queryName) {
            return pair[1];
        }
    }
    return "";
}