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
    } else {
        initIndex();
    }
})

function initLoggedOut() {
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
                        debugger
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
    $("#logoutId").val(logoutId);
}

function initIndex() {
    $.get("account/session", (res) => {
        if (res.data && res.data.sub) {
            const user = $("#user");
            user.attr("sub", res.data.sub);
            user.text(res.data.name);
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