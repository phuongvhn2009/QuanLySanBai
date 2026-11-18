// Plugin chạy 1 lần (chạy lại được, không làm hỏng dữ liệu) để chuẩn hóa file Figma "Quanr ly san bai":
//  1. Gom toàn bộ mã màu / bo góc / font đang gõ tay thành Variables tiếng Việt dễ hiểu trong bộ "Sân Bãi Tokens".
//  2. Gắn (bind) mọi fill, stroke, bo góc của layer vào Variables đó.
//  3. Tạo Text Styles "Chữ/..." để đồng bộ cỡ chữ và font chữ.
//  4. Khóa các layer trang trí ("Đường phân cách", "Khoảng trống") để người dùng không bấm nhầm làm lệch layout.
//  5. Tạo khung "🎨 Design Tokens" & "📖 Hướng dẫn chỉnh sửa nhanh" ngay trên canvas.
// Không đụng vào mã nguồn hệ thống (C# / Razor / CSS).

var COLLECTION = "Sân Bãi Tokens";
var FRAME_NAME = "🎨 Design Tokens";
var GUIDE_FRAME_NAME = "📖 Hướng dẫn chỉnh sửa nhanh";

var COLOR_NAMES = {
  "080808": "Màu sắc / Nền trang",
  "0B0B0B": "Màu sắc / Nền thanh điều hướng",
  "181818": "Màu sắc / Nền thẻ & ô nhập",
  "343434": "Màu sắc / Đường viền",
  "F5F5F1": "Màu sắc / Chữ chính",
  "B3B3B3": "Màu sắc / Chữ phụ",
  "CCCCCC": "Màu sắc / Chữ menu",
  "E50914": "Màu sắc / Thương hiệu (Chính)",
  "FF5961": "Màu sắc / Thương hiệu (Chữ nổi)",
  "321719": "Màu sắc / Thương hiệu (Nền nhạt)",
  "341317": "Màu sắc / Thương hiệu (Nền nhạt 2)",
  "14251B": "Trạng thái / Thành công (Nền)",
  "75DEA2": "Trạng thái / Thành công (Chữ)",
  "352511": "Trạng thái / Chờ duyệt (Nền)",
  "FFD28A": "Trạng thái / Chờ duyệt (Chữ)",
  "351317": "Trạng thái / Từ chối (Nền)",
  "FF8B91": "Trạng thái / Từ chối (Chữ)",
  "132639": "Trạng thái / Thông tin (Nền)",
  "9CCEFF": "Trạng thái / Thông tin (Chữ)"
};

var RADIUS_NAMES = {
  4: "Bo góc / Nhỏ (4px)",
  6: "Bo góc / Vừa (6px)",
  8: "Bo góc / Lớn (8px)"
};

var CORNERS = ["topLeftRadius", "topRightRadius", "bottomLeftRadius", "bottomRightRadius"];
var DECORATIVE_NAMES = ["Đường phân cách", "Khoảng trống"];

function toHex(c) {
  return [c.r, c.g, c.b].map(function (v) {
    return Math.round(v * 255).toString(16).padStart(2, "0").toUpperCase();
  }).join("");
}

function lineHeightLabel(lh) {
  if (lh.unit === "PERCENT") return (Math.round(lh.value) / 100) + "em";
  if (lh.unit === "PIXELS") return lh.value + "px";
  return "auto";
}

async function loadFirstFont(candidates) {
  for (var i = 0; i < candidates.length; i++) {
    try {
      await figma.loadFontAsync(candidates[i]);
      return candidates[i];
    } catch (e) {
      /* thử font kế tiếp */
    }
  }
  throw new Error("Không tải được font nào để vẽ khung hướng dẫn.");
}

async function main() {
  await figma.loadAllPagesAsync();

  // ---- 1. Tạo / đọc bộ Variables ----
  var collections = await figma.variables.getLocalVariableCollectionsAsync();
  var col = collections.find(function (c) { return c.name === COLLECTION; }) ||
    figma.variables.createVariableCollection(COLLECTION);
  var modeId = col.modes[0].modeId;
  var vars = new Map();

  (await figma.variables.getLocalVariablesAsync()).forEach(function (v) {
    if (v.variableCollectionId === col.id) vars.set(v.name, v);
  });

  // Không ghi đè giá trị đã có nếu người dùng đã tự chỉnh trong Figma
  function getVar(name, type, value) {
    var v = vars.get(name);
    if (!v) {
      v = figma.variables.createVariable(name, col, type);
      v.setValueForMode(modeId, value);
      vars.set(name, v);
    }
    return v;
  }

  var fontVar = getVar("Font chữ / Chính", "STRING", "Be Vietnam Pro");

  // Xóa khung tokens & hướng dẫn cũ (sẽ vẽ lại gọn đẹp ở cuối)
  figma.root.children.forEach(function (page) {
    page.children.filter(function (n) {
      return n.name === FRAME_NAME || n.name === GUIDE_FRAME_NAME;
    }).forEach(function (n) { n.remove(); });
  });

  // Component gốc xử lý trước để instance tự kế thừa, tránh tạo override thừa
  function rank(node) {
    for (var p = node; p && p.type !== "PAGE"; p = p.parent) {
      if (p.type === "COMPONENT" || p.type === "COMPONENT_SET") return 0;
      if (p.type === "INSTANCE") return 2;
    }
    return 1;
  }

  var nodes = [];
  figma.root.children.forEach(function (page) { nodes = nodes.concat(page.findAll()); });
  nodes.sort(function (a, b) { return rank(a) - rank(b); });

  var stats = { paints: 0, radius: 0, text: 0, locked: 0, skipped: 0 };

  function bindPaints(node, prop) {
    var paints = node[prop];
    if (!Array.isArray(paints) || paints.length === 0) return;
    var changed = 0;
    var next = paints.map(function (p) {
      if (p.type !== "SOLID" || (p.boundVariables && p.boundVariables.color)) return p;
      var hex = toHex(p.color);
      var varName = COLOR_NAMES[hex] || ("Màu sắc / Khác (" + hex + ")");
      var v = getVar(varName, "COLOR", { r: p.color.r, g: p.color.g, b: p.color.b, a: 1 });
      changed++;
      return figma.variables.setBoundVariableForPaint(p, "color", v);
    });
    if (changed) {
      node[prop] = next;
      stats.paints += changed;
    }
  }

  function bindRadius(node) {
    if (!("cornerRadius" in node) || typeof node.cornerRadius !== "number" || node.cornerRadius <= 0) return;
    if (node.boundVariables && node.boundVariables.topLeftRadius) return;
    var r = node.cornerRadius;
    var v = getVar(RADIUS_NAMES[r] || ("Bo góc / " + r + "px"), "FLOAT", r);
    CORNERS.forEach(function (f) { node.setBoundVariable(f, v); });
    stats.radius++;
  }

  // ---- 2. Text Styles ----
  var styles = new Map();
  (await figma.getLocalTextStylesAsync()).forEach(function (s) { styles.set(s.name, s); });

  async function styleFor(node) {
    var lh = lineHeightLabel(node.lineHeight);
    var ls = node.letterSpacing;
    var name = "Chữ/" + node.fontSize + " " + node.fontName.style +
      (lh !== "1.5em" ? " · " + lh : "") +
      (ls.value ? " · ls " + ls.value + (ls.unit === "PERCENT" ? "%" : "px") : "");
    var s = styles.get(name);
    if (!s) {
      s = figma.createTextStyle();
      s.name = name;
      s.fontName = node.fontName;
      s.fontSize = node.fontSize;
      s.lineHeight = node.lineHeight;
      s.letterSpacing = node.letterSpacing;
      try { s.setBoundVariable("fontFamily", fontVar); } catch (e) { /* bỏ qua nếu Figma chưa hỗ trợ */ }
      styles.set(name, s);
    }
    return s;
  }

  async function bindText(node) {
    if (node.textStyleId !== "" || node.fontName === figma.mixed || node.fontSize === figma.mixed ||
      node.lineHeight === figma.mixed || node.letterSpacing === figma.mixed) return;
    await figma.loadFontAsync(node.fontName);
    var s = await styleFor(node);
    await node.setTextStyleIdAsync(s.id);
    stats.text++;
  }

  for (var i = 0; i < nodes.length; i++) {
    var n = nodes[i];
    try {
      bindPaints(n, "fills");
      bindPaints(n, "strokes");
      bindRadius(n);
      if (n.type === "TEXT") await bindText(n);
      if (DECORATIVE_NAMES.indexOf(n.name) !== -1 && !n.locked) {
        n.locked = true;
        stats.locked++;
      }
    } catch (e) {
      stats.skipped++;
    }
  }

  // ---- 3. Vẽ khung "🎨 Design Tokens" & "📖 Hướng dẫn chỉnh sửa nhanh" ----
  var labelFont = await loadFirstFont([
    { family: "Be Vietnam Pro", style: "Regular" },
    { family: "Inter", style: "Regular" }
  ]);
  var boldFont = await loadFirstFont([
    { family: "Be Vietnam Pro", style: "Bold" },
    { family: "Inter", style: "Bold" },
    labelFont
  ]);

  var page = figma.currentPage;
  var right = page.children.reduce(function (m, c) { return Math.max(m, c.x + c.width); }, 0);

  var bgVar = vars.get("Màu sắc / Nền trang");
  var cardVar = vars.get("Màu sắc / Nền thẻ & ô nhập");
  var borderVar = vars.get("Màu sắc / Đường viền");
  var textPrimaryVar = vars.get("Màu sắc / Chữ chính");
  var textSecVar = vars.get("Màu sắc / Chữ phụ");
  var brandVar = vars.get("Màu sắc / Thương hiệu (Chính)");

  function makeText(text, size, isBold, colorVar, fallbackRgb) {
    var t = figma.createText();
    t.fontName = isBold ? boldFont : labelFont;
    t.fontSize = size || 13;
    t.characters = text;
    var paint = { type: "SOLID", color: fallbackRgb || { r: 0.9, g: 0.9, b: 0.9 } };
    t.fills = [colorVar ? figma.variables.setBoundVariableForPaint(paint, "color", colorVar) : paint];
    return t;
  }

  // Khung Design Tokens
  var frame = figma.createFrame();
  frame.name = FRAME_NAME;
  frame.layoutMode = "VERTICAL";
  frame.primaryAxisSizingMode = "AUTO";
  frame.counterAxisSizingMode = "AUTO";
  frame.itemSpacing = 10;
  frame.paddingTop = frame.paddingBottom = frame.paddingLeft = frame.paddingRight = 24;
  frame.cornerRadius = 8;
  frame.x = right + 200;
  frame.y = 0;
  page.appendChild(frame);
  if (bgVar) {
    frame.fills = [figma.variables.setBoundVariableForPaint({ type: "SOLID", color: { r: 0, g: 0, b: 0 } }, "color", bgVar)];
  }
  if (borderVar) {
    frame.strokes = [figma.variables.setBoundVariableForPaint({ type: "SOLID", color: { r: 0.2, g: 0.2, b: 0.2 } }, "color", borderVar)];
    frame.strokeWeight = 1;
  }

  frame.appendChild(makeText("🎨 Bảng Design Tokens (" + COLLECTION + ")", 18, true, textPrimaryVar));
  frame.appendChild(makeText("Sửa màu sắc, bo góc, font ở mục Variables bên trái — toàn bộ thiết kế sẽ tự đổi theo.", 13, false, textSecVar));

  var sorted = Array.from(vars.values()).sort(function (a, b) { return a.name.localeCompare(b.name); });
  sorted.forEach(function (v) {
    if (v.resolvedType === "STRING") return;
    var row = figma.createFrame();
    row.name = "Hàng / " + v.name;
    row.layoutMode = "HORIZONTAL";
    row.primaryAxisSizingMode = "AUTO";
    row.counterAxisSizingMode = "AUTO";
    row.counterAxisAlignItems = "CENTER";
    row.itemSpacing = 12;
    row.fills = [];

    var sw = figma.createRectangle();
    sw.name = v.name;
    sw.resize(36, 36);
    if (v.resolvedType === "COLOR") {
      sw.fills = [figma.variables.setBoundVariableForPaint({ type: "SOLID", color: { r: 0, g: 0, b: 0 } }, "color", v)];
      sw.strokes = [{ type: "SOLID", color: { r: 0.3, g: 0.3, b: 0.3 } }];
      sw.cornerRadius = 4;
    } else {
      var rPaint = { type: "SOLID", color: { r: 0.9, g: 0.04, b: 0.08 } };
      sw.fills = [brandVar ? figma.variables.setBoundVariableForPaint(rPaint, "color", brandVar) : rPaint];
      CORNERS.forEach(function (f) { sw.setBoundVariable(f, v); });
    }
    row.appendChild(sw);
    row.appendChild(makeText(v.name, 13, false, textPrimaryVar));
    frame.appendChild(row);
  });

  var fontText = figma.createText();
  fontText.name = "Font chữ / Chính";
  var fam = String(fontVar.valuesByMode[modeId]);
  var famFont = await loadFirstFont([{ family: fam, style: "Regular" }, labelFont]);
  fontText.fontName = famFont;
  fontText.fontSize = 15;
  fontText.characters = "Font chữ chính: " + fam + " — Aa Bb Cc Đặt sân nhanh";
  var fp = { type: "SOLID", color: { r: 0.96, g: 0.96, b: 0.95 } };
  fontText.fills = [textPrimaryVar ? figma.variables.setBoundVariableForPaint(fp, "color", textPrimaryVar) : fp];
  try { fontText.setBoundVariable("fontFamily", fontVar); } catch (e) { /* bỏ qua nếu Figma chưa hỗ trợ */ }
  frame.appendChild(fontText);

  // Khung Hướng dẫn chỉnh sửa nhanh
  var guide = figma.createFrame();
  guide.name = GUIDE_FRAME_NAME;
  guide.layoutMode = "VERTICAL";
  guide.primaryAxisSizingMode = "AUTO";
  guide.counterAxisSizingMode = "AUTO";
  guide.itemSpacing = 12;
  guide.paddingTop = guide.paddingBottom = guide.paddingLeft = guide.paddingRight = 24;
  guide.cornerRadius = 8;
  guide.x = frame.x;
  guide.y = frame.y + frame.height + 32;
  page.appendChild(guide);
  if (cardVar) {
    guide.fills = [figma.variables.setBoundVariableForPaint({ type: "SOLID", color: { r: 0.1, g: 0.1, b: 0.1 } }, "color", cardVar)];
  }
  if (borderVar) {
    guide.strokes = [figma.variables.setBoundVariableForPaint({ type: "SOLID", color: { r: 0.2, g: 0.2, b: 0.2 } }, "color", borderVar)];
    guide.strokeWeight = 1;
  }

  guide.appendChild(makeText("📖 Hướng dẫn chỉnh sửa nhanh cho người dùng", 16, true, textPrimaryVar));
  guide.appendChild(makeText("1. Đổi màu toàn bộ giao diện: Bấm biểu tượng 'Variables' (thanh bên trái) -> mở bộ 'Sân Bãi Tokens' -> chọn màu muốn đổi.", 13, false, textSecVar));
  guide.appendChild(makeText("2. Sửa chữ trên nút / thẻ: Bấm chọn nút hoặc thẻ -> sửa trực tiếp tại mục thuộc tính bên phải (không cần phá khung Auto-Layout).", 13, false, textSecVar));
  guide.appendChild(makeText("3. Đổi cỡ chữ hàng loạt: Chọn mục 'Styles' bên phải -> bấm biểu tượng chỉnh sửa cạnh style 'Chữ/...'.", 13, false, textSecVar));
  guide.appendChild(makeText("4. Các đường phân cách & khoảng trống đã được khóa tự động để tránh kéo nhầm làm lệch giao diện.", 13, false, textSecVar));

  figma.viewport.scrollAndZoomIntoView([frame, guide]);
  figma.notify(
    "Hoàn tất! Đã gắn " + stats.paints + " màu, " + stats.radius + " bo góc, " +
    stats.text + " chữ, khóa " + stats.locked + " layer trang trí" +
    (stats.skipped ? " (" + stats.skipped + " layer bỏ qua)" : "") + ".",
    { timeout: 8000 }
  );
}

main().then(
  function () { figma.closePlugin(); },
  function (e) { figma.closePlugin("Lỗi: " + (e && e.message ? e.message : e)); }
);
