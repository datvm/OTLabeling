import initSqlJs from "https://cdn.jsdelivr.net/npm/sql.js@1.12.0/+esm";

const sqlWasmUrl = "https://cdn.jsdelivr.net/npm/sql.js@1.12.0/dist/sql-wasm.wasm";
const databaseUrl = "labeling.sqlite";

let db;

export async function beforeStart() {
    const SQL = await initSqlJs({
        locateFile: () => sqlWasmUrl
    });
    const response = await fetch(databaseUrl);
    if (!response.ok)
        throw new Error("Could not load " + databaseUrl + " (" + response.status + ").");

    const bytes = new Uint8Array(await response.arrayBuffer());
    db = new SQL.Database(bytes);

    globalThis.appDb = {
        query(sql, params) {
            if (!db)
                throw new Error("Database is not open.");

            const statement = db.prepare(sql);
            try {
                if (params && params.length)
                    statement.bind(params);

                const rows = [];
                while (statement.step())
                    rows.push(statement.getAsObject());

                return rows;
            } finally {
                statement.free();
            }
        }
    };
}
