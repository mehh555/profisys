import { useState } from "react";
import { DocumentDetailModal } from "./components/DocumentDetailModal";
import { DocumentsTable } from "./components/DocumentsTable";

function App() {
  const [selectedDocumentId, setSelectedDocumentId] = useState<number | null>(null);

  return (
    <>
      <DocumentsTable onRowClick={setSelectedDocumentId} />
      <DocumentDetailModal
        documentId={selectedDocumentId}
        onClose={() => setSelectedDocumentId(null)}
      />
    </>
  );
}

export default App;
