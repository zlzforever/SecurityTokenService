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
               var = substr($0, RSTART + 2, RLENGTH - 3)
               # 只替换【当前匹配到的这一个】变量，而不是全部替换
               before = substr($0, 1, RSTART - 1)
               after = substr($0, RSTART + RLENGTH)
               $0 = before ENVIRON[var] after
           }
           print
       }' "$input_file" > "$output_file"
       echo "配置文件已生成"
    else
       echo "使用默认配置文件"
    fi
}

if [ -z "$CONFIG_SOURCE" ]; then
    echo "环境变量 CONFIG_SOURCE 不存在， 使用默认配置文件"
else
    generate "${CONFIG_SOURCE}" "/app/appsettings.json"
fi

exec "$@"