#!/bin/bash
# Tạo database từ DB/Quanlykhachsanc#.sql nếu chưa có (lần chạy sau giữ nguyên dữ liệu)
set -e
SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S db -U sa -P $SA_PASSWORD -C -b"
if [ "$($SQLCMD -h -1 -W -Q "SET NOCOUNT ON; SELECT DB_ID(N'QuanLyKhachSanc#')")" = "NULL" ]; then
  echo "Chưa có database QuanLyKhachSanc#: chạy script tạo bảng + dữ liệu mẫu..."
  $SQLCMD -f 65001 -i "/db/Quanlykhachsanc#.sql" > /tmp/db-init.log || { tail -20 /tmp/db-init.log; exit 1; }
  echo "Đã tạo database."
else
  echo "Database QuanLyKhachSanc# đã có, bỏ qua (API tự nâng cấp cấu trúc khi khởi động)."
fi
