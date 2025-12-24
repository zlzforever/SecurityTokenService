#!/bin/bash

# 在这里添加你需要执行的命令
# 例如，运行数据库迁移、配置检查等
fc-cache -f -v

generate() {
    # 输入文件名
    input_file="$1"
    # 输出文件名
    output_file="$2"
    
    # 检查输入文件是否存在
    if [ -f "${input_file}" ]; then
       awk '{
           while (match($0, /\$\{[A-Za-z_][A-Za-z0-9_]*\}/)) {
               var=substr($0, RSTART+2, RLENGTH-3)
               gsub(/\$\{[A-Za-z_][A-Za-z0-9_]*\}/, ENVIRON[var])
           }
           print
       }' "${input_file}" >"${output_file}"
       echo "配置文件 ${output_file} 已生成"
    else
       echo "使用默认配置文件 ${output_file}"
    fi
}

if [ -z "$CONFIG_SOURCE" ]; then
    echo "环境变量 CONFIG_SOURCE 不存在， 使用默认配置文件"
else
    generate "${CONFIG_SOURCE}" "/app/appsettings.json"
fi

exec "$@"