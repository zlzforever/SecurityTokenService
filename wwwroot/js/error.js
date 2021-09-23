$(document).ready(() => {
    const errorId = GetQueryValue('errorId');
    switch (errorId) {
        case "4001": {
            $('#message').text('不支持双因素认证')
            break;
        }
        case "4002": {
            $('#message').text('用户被禁止登录')
            break;
        }
        case "4003": {
            $('#message').text('用户被锁定')
            break;
        }
        case "4004": {
            $('#message').text('用户名或密码不正确')
            break;
        }
        case "4005": {
            $('#message').text('不支持 NativeClient')
            break;
        }
        case "4006": {
            $('#message').text('返回地址不合法')
            break;
        }
    }
})

