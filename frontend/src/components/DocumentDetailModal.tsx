import { Descriptions, message, Modal, Spin, Table } from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs from "dayjs";
import { useEffect, useMemo, useState } from "react";
import { getDocumentById } from "../api/documentsApi";
import type { DocumentDetail, DocumentItem } from "../types/document";

interface Props {
  documentId: number | null;
  onClose: () => void;
}

export function DocumentDetailModal({ documentId, onClose }: Props) {
  const [document, setDocument] = useState<DocumentDetail | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (documentId === null) {
      setDocument(null);
      return;
    }

    const controller = new AbortController();
    let cancelled = false;

    setLoading(true);
    getDocumentById(documentId, controller.signal)
      .then((data) => {
        if (!cancelled) setDocument(data);
      })
      .catch((err) => {
        if (cancelled || controller.signal.aborted) return;
        if (err instanceof DOMException && err.name === "AbortError") return;
        message.error(err instanceof Error ? err.message : "Failed to load document details");
        setDocument(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [documentId]);

  const itemsWithGross = useMemo(() => {
    if (!document) return [];
    return document.items.map((item) => ({
      ...item,
      grossValue: item.price * (1 + item.taxRate / 100),
    }));
  }, [document]);

  const columns = useMemo<ColumnsType<DocumentItem & { grossValue: number }>>(
    () => [
      { title: "Ordinal", dataIndex: "ordinal", key: "ordinal", width: 80 },
      { title: "Product", dataIndex: "product", key: "product" },
      { title: "Quantity", dataIndex: "quantity", key: "quantity", width: 100 },
      {
        title: "Price",
        dataIndex: "price",
        key: "price",
        width: 120,
        render: (value: number) => value.toFixed(2),
      },
      {
        title: "Tax Rate",
        dataIndex: "taxRate",
        key: "taxRate",
        width: 100,
        render: (value: number) => `${value}%`,
      },
      {
        title: "Gross Value",
        dataIndex: "grossValue",
        key: "grossValue",
        width: 140,
        render: (value: number) => value.toFixed(2),
      },
    ],
    []
  );

  return (
    <Modal
      title={`Document #${documentId}`}
      open={documentId !== null}
      onCancel={onClose}
      footer={null}
      width={900}
    >
      {loading ? (
        <Spin />
      ) : document ? (
        <>
          <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Id">{document.id}</Descriptions.Item>
            <Descriptions.Item label="Type">{document.type}</Descriptions.Item>
            <Descriptions.Item label="Date">
              {dayjs(document.date).format("YYYY-MM-DD")}
            </Descriptions.Item>
            <Descriptions.Item label="City">{document.city}</Descriptions.Item>
            <Descriptions.Item label="First Name">{document.firstName}</Descriptions.Item>
            <Descriptions.Item label="Last Name">{document.lastName}</Descriptions.Item>
          </Descriptions>
          <Table
            columns={columns}
            dataSource={itemsWithGross}
            rowKey="id"
            pagination={false}
            size="small"
          />
        </>
      ) : null}
    </Modal>
  );
}
