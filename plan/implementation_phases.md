# 📅 Detailed Implementation Phases- [ ] **Action:** Simplify ItemTemplates to reduce visual tree complexity per item.
This document expands the logic from `update.md` into 12 distinct, actionable phases.

## 🧹 Phase 1: Namespace & Import Cleanup
**Goal:** Standardize code structure and reduce visual clutter.
- [ ] **Action:** Run "Remove and Sort Usings" on all `.cs` files.
- [ ] **Action:** Convert block-scoped namespaces (`namespace ColorPicker { ... }`) to file-scoped namespaces (`namespace ColorPicker;`).
- [ ] **Action:** Verify no logic breakage after restructure.

## 🗑️ Phase 2: Dead Code Elimination
**Goal:** Remove distractions and maintain clarity.
- [ ] **Action:** Scan for and delete large blocks of commented-out code.
- [ ] **Action:** Identify unused helper methods in `ProjectHelper.cs` or similar services.
- [ ] **Action:** Remove any unused assets from the `Assets` folder.

## ⚠️ Phase 3: Static Analysis & Warning Fixes
**Goal:** Achieve a clean build with zero warnings.
- [ ] **Action:** Enable "Treat Warnings as Errors" temporarily to catch everything.
- [ ] **Action:** Fix `CS8618` (Non-nullable field uninitialized) in Models/ViewModels.
- [ ] **Action:** Fix `CS8602` (Dereference of a possibly null reference) with proper null checks.
- [ ] **Review:** Ensure no "bang" operators (`!`) are used wildly without justification.

## ⚡ Phase 4: Async/Await Audit
**Goal:** Prevent UI freezes.
- [ ] **Action:** Grep for `.Result`, `.Wait()`, and `Thread.Sleep`.
- [ ] **Action:** Refactor synchronous file/database IO to use `await`.
- [ ] **Action:** Configure `ConfigureAwait(false)` for library code where appropriate.

## 🖼️ Phase 5: Image Handling Optimization
**Goal:** Reduce memory footprint.
- [ ] **Action:** Implement `DecodePixelWidth` / `DecodePixelHeight` for thumbnail generation in `ImagePalettePage`.
- [ ] **Action:** Ensure main images are loaded asynchronously.
- [ ] **Action:** Verify disposal of stream resources after image load.

## 🔄 Phase 6: Resource Lifecycle Management
**Goal:** Prevent memory leaks.
- [ ] **Action:** Implement `IDisposable` in Services with active listeners (Firebase).- [ ] **Action:** Unsubscribe from events in `OnNavigatedFrom` in Pages.
- [ ] **Action:** Fix `CancellationToken` propagation in long-running tasks.

## 📜 Phase 7: UI Virtualization & Rendering
**Goal:** Smooth scrolling for large lists.
- [ ] **Action:** Verify `ScrollViewer` logic in `ColorHistory` list.
- [ ] **Action:** Ensure `ItemsStackPanel` is used correctly for virtualization.
- [ ] **Action:** simplify ItemTemplates to reduce visual tree complexity per item.

## 🎨 Phase 8: Design System Foundation
**Goal:** Centralize styles and colors.
- [ ] **Action:** Create `Styles/Colors.xaml` and `Styles/Fonts.xaml`.
- [ ] **Action:** Define semantic names (e.g., `AppBackgroundBrush`, `PrimaryActionBrush`) instead of hardcoded hex values.
- [ ] **Action:** Update `App.xaml` to merge these dictionaries.

## 🪟 Phase 9: Modern Windowing (Mica/Acrylic)
**Goal:** Native Windows 11 feel.
- [ ] **Action:** Enable **Mica** backdrop in `MainWindow`.
- [ ] **Action:** Extend content into the title bar (`ExtendsContentIntoTitleBar = true`).
- [ ] **Action:** Create a custom caption button implementation if needed for the extended title bar.

## 💅 Phase 10: Component Styling (Fluent Design)
**Goal:** Consistent approachable UI.
- [ ] **Action:** Apply `CornerRadius="4"` or `8` to all Buttons, TextBoxes, and Panels.
- [ ] **Action:** Update `Button` styles to use `RevealHighlight` or modern hover effects.
- [ ] **Action:** Style `TextBox` and `ComboBox` to match WinUI 3 guidelines (even if using WPF/UWP).

## 🏃 Phase 11: Motion & Micro-interactions
**Goal:** Delightful feedback.
- [ ] **Action:** Add `PointerEntered`/`PointerExited` scale animations on cards.
- [ ] **Action:** Implement **Connected Animations** when clicking a palette to view details.
- [ ] **Action:** Add Lottie or progress ring animations for loading states.

## 🔍 Phase 12: Final Polish & Verification
**Goal:** Launch readiness.
- [ ] **Action:** Standardize Iconography to `Segoe Fluent Icons`.
- [ ] **Action:** Validate Dark/Light mode switching.
- [ ] **Action:** Full regression test of Color Picker and Image Extractor.
