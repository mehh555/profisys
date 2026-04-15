import { Alert, Button, Card, Col, DatePicker, Input, message, Row, Select, Space, Table, Upload } from "antd";
import type { ColumnsType, TableProps } from "antd/es/table";
import type { SorterResult } from "antd/es/table/interface";
import type { RcFile } from "antd/es/upload";
import dayjs from "dayjs";
import { useState } from "react";
import { exportDocumentsUrl, importDocuments } from "../api/documentsApi";
import { useDocuments } from "../hooks/useDocuments";
import type { DocumentListItem } from "../types/document";

const { RangePicker } = DatePicker;

interface Props {
  onRowClick: (id: number) => void;
}

export function DocumentsTable({ onRowClick }: Props) {
  const {
    documents,
    totalCount,
    loading,
    error,
    filter,
    setPage,
    setSearch,
    setType,
    setDateRange,
    setSort,
    refresh,
  } = useDocuments();

  const [importing, setImporting] = useState(false);
  const [documentsFile, setDocumentsFile] = useState<RcFile | null>(null);
  const [itemsFile, setItemsFile] = useState<RcFile | null>(null);

  const handleImport = async () => {
    if (!documentsFile || !itemsFile) return;
    setImporting(true);
    try {
      const result = await importDocuments(documentsFile, itemsFile);
      const parts = [`Imported ${result.imported}`, `updated ${result.updated}`];
      if (result.skippedItems > 0) parts.push(`skipped ${result.skippedItems} orphaned items`);
      message.success(parts.join(", "));
      setDocumentsFile(null);
      setItemsFile(null);
      refresh();
    } catch (err) {
      message.error(err instanceof Error ? err.message : "Import failed");
    } finally {
      setImporting(false);
    }
  };

  const handleTableChange: TableProps<DocumentListItem>["onChange"] = (_pagination, _filters, sorter) => {
    const s = sorter as SorterResult<DocumentListItem>;
    if (s.field && s.order) {
      const sortDir = s.order === "ascend" ? "asc" : "desc";
      setSort(String(s.field), sortDir);
    } else {
      setSort(undefined, undefined);
    }
  };

  const handleExport = () => {
    const link = document.createElement("a");
    link.href = exportDocumentsUrl(filter);
    link.download = "documents_export.csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const columns: ColumnsType<DocumentListItem> = [
    { title: "Id", dataIndex: "id", key: "id", width: 70, sorter: true },
    { title: "Type", dataIndex: "type", key: "type", width: 100, sorter: true },
    {
      title: "Date",
      dataIndex: "date",
      key: "date",
      width: 110,
      sorter: true,
      render: (value: string) => dayjs(value).format("YYYY-MM-DD"),
    },
    { title: "First Name", dataIndex: "firstName", key: "firstName", sorter: true },
    { title: "Last Name", dataIndex: "lastName", key: "lastName", sorter: true },
    { title: "City", dataIndex: "city", key: "city", sorter: true },
  ];

  return (
    <div style={{ padding: 24 }}>
      {error && (
        <Alert
          message="Failed to load documents"
          description={error}
          type="error"
          showIcon
          closable
          style={{ marginBottom: 16 }}
        />
      )}

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={24} lg={14}>
          <Card size="small" title="Filters">
            <Space wrap>
              <Input.Search
                placeholder="Search by name or city"
                allowClear
                onSearch={setSearch}
                style={{ width: 240 }}
              />
              <Select
                placeholder="Filter by type"
                allowClear
                onChange={(value) => setType(value)}
                style={{ width: 150 }}
                options={[
                  { label: "Invoice", value: "Invoice" },
                  { label: "Order", value: "Order" },
                  { label: "Receipt", value: "Receipt" },
                ]}
              />
              <RangePicker
                onChange={(dates) => {
                  if (dates && dates[0] && dates[1]) {
                    setDateRange(
                      dates[0].format("YYYY-MM-DD"),
                      dates[1].format("YYYY-MM-DD")
                    );
                  } else {
                    setDateRange(undefined, undefined);
                  }
                }}
              />
            </Space>
          </Card>
        </Col>

        <Col xs={24} lg={10}>
          <Card size="small" title="Import / Export">
            <Space wrap>
              <Upload
                accept=".csv"
                beforeUpload={(file) => { setDocumentsFile(file); return false; }}
                onRemove={() => setDocumentsFile(null)}
                fileList={documentsFile ? [documentsFile] : []}
                maxCount={1}
              >
                <Button>Documents.csv</Button>
              </Upload>
              <Upload
                accept=".csv"
                beforeUpload={(file) => { setItemsFile(file); return false; }}
                onRemove={() => setItemsFile(null)}
                fileList={itemsFile ? [itemsFile] : []}
                maxCount={1}
              >
                <Button>DocumentItems.csv</Button>
              </Upload>
              <Button
                type="primary"
                onClick={handleImport}
                loading={importing}
                disabled={!documentsFile || !itemsFile}
              >
                Import
              </Button>
              <Button onClick={handleExport}>Export CSV</Button>
            </Space>
          </Card>
        </Col>
      </Row>

      <Table
        columns={columns}
        dataSource={documents}
        rowKey="id"
        loading={loading}
        pagination={{
          current: filter.page,
          pageSize: filter.pageSize,
          total: totalCount,
          showSizeChanger: true,
          onChange: setPage,
        }}
        onChange={handleTableChange}
        onRow={(record) => ({
          onClick: () => onRowClick(record.id),
          style: { cursor: "pointer" },
        })}
      />
    </div>
  );
}
