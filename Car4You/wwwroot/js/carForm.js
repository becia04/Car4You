$(function () {
    // --- Ustal tryb formularza ---
    const carId = parseInt($('#Car_Id').val(), 10) || 0;
    let isEditMode = false;
    if (!carId || carId === 0) {
        isEditMode = false; // dodawanie
    } else {
        isEditMode = true; // edycja
    }
    const errorsMap = {};

    function setError(key, message) {
        if (message && message.length) errorsMap[key] = message;
        else delete errorsMap[key];
        renderGlobalErrors();
    }

    // elementy
    const errorDiv = $('#formErrors');
    const submitBtn = $('#submitCarBtn');

    // --- Renderowanie globalnych błędów ---
    function renderGlobalErrors() {
        const msgs = Object.values(errorsMap || {});
        const html = msgs.join('<br>');
        errorDiv.html(html);

        // przycisk nieaktywny, jeśli są błędy
        const hasErrors = msgs.length > 0;
        submitBtn.prop('disabled', hasErrors);
    }

    // --- Pokazywanie/ukrywanie div'a z błędami przy hoverze ---
    submitBtn.off('mouseenter.formErr mouseleave.formErr');
    submitBtn.on('mouseenter.formErr', function (e) {
        const msgs = Object.values(errorsMap || {});
        if (msgs.length === 0) return; // brak błędów → nie pokazuj nic

        // pozycjonowanie względem przycisku
        const btnOffset = submitBtn.offset();
        const btnHeight = submitBtn.outerHeight();
        const offsetY = btnOffset.top - errorDiv.outerHeight() - 8;
        const offsetX = btnOffset.left;

        const spaceAbove = btnOffset.top;
        if (spaceAbove < errorDiv.outerHeight() + 20) {
            // mało miejsca nad przyciskiem → pokaż pod
            errorDiv.css({
                top: (btnOffset.top + btnHeight + 8) + 'px',
                left: offsetX + 'px',
                position: 'absolute',
                display: 'block'
            });
        } else {
            // pokaż nad przyciskiem
            errorDiv.css({
                top: offsetY + 'px',
                left: offsetX + 'px',
                position: 'absolute',
                display: 'block'
            });
        }
    });

    submitBtn.on('mouseleave.formErr', function () {
        errorDiv.hide();
    });

    // --- Zmiana marki ---
    $('#BrandId').on('change', function () {
        const val = $(this).val() || "0";
        $('#SelectedBrandId').val(val);
        let brandError = '';
        let modelError = '';

        if (val === "0") {
            $('#CarModelId, #VersionId').prop('disabled', true).empty();
            $('#addModelBtn, #addVersionBtn').prop('disabled', true);
            $('#SelectedCarModelId, #SelectedCarVersionId').val("0");
            setError('model', 'Wybierz model samochodu');
            brandError = "Wybierz markę samochodu";
        } else {
            $('#CarModelId').prop('disabled', false);
            $('#addModelBtn').prop('disabled', false);
            $('#CarModelId, #SelectedCarModelId').val("0");
            $('#VersionId').prop('disabled', true).empty().append('<option value="">Wybierz generację</option>');
            $('#SelectedCarVersionId').val("0");
            $('#addVersionBtn').prop('disabled', true);
            loadModelsForBrand(val, false);
            brandError = '';
            modelError = "Wybierz model samochodu";
        }
        setError('brand', brandError);
        setError('model', modelError);
        $('#BrandError').text(brandError);
        $('#CarModelError').text(modelError);

        updateTitle();
    });

    // --- Funkcje ładowania modeli i wersji ---
    async function loadModelsForBrand(brandId, selectPrevious = false) {
        try {
            const response = await $.ajax({ url: '/Admin/GetCarModelsByBrand', data: { brandId }, type: 'GET' });
            response.sort((a, b) => a.name.localeCompare(b.name, 'pl', { numeric: true, sensitivity: 'base' }));
            const selectedModelId = selectPrevious ? ($('#SelectedCarModelId').val() || "0") : "0";
            const modelSelect = $('#CarModelId'); modelSelect.empty().append('<option value="">Wybierz model</option>');
            $.each(response, (i, item) => { modelSelect.append(`<option value="${item.id}" ${String(item.id) === String(selectedModelId) ? 'selected' : ''}>${item.name}</option>`); });
            modelSelect.prop('disabled', false); $('#addModelBtn').prop('disabled', false);
            if (selectPrevious && selectedModelId !== "0") { setTimeout(() => { modelSelect.val(selectedModelId).trigger('change'); }, 100); }
        } catch (e) { console.error(e); alert("Błąd podczas ładowania modeli."); }
    }

    // --- Zmiana modelu ---
    $('#CarModelId').on('change', function () {
        const val = $(this).val() || "0";
        $('#SelectedCarModelId').val(val);
        let CarModelError = '';

        if (val === "0") {
            $('#VersionId').prop('disabled', true).empty().append('<option value="">Wybierz generację</option>');
            $('#SelectedCarVersionId').val("0");
            $('#addVersionBtn').prop('disabled', true);
            CarModelError = "Wybierz model samochodu";
        } else {
            CarModelError = '';
            loadVersionsForModel(val);
        }
        setError('model', CarModelError);
        $('#CarModelError').text(CarModelError);
        updateTitle();
    });

    // Dodawanie nowego modelu
    window.addNewModel = function () {
        let modelName = $("#newModelName").val();
        let brandId = $("#BrandId").val();
        console.log(modelName, brandId)
        if (!modelName) {
            alert("Podaj nazwę modelu!");
            return;
        }
        if (!brandId) {
            alert("Brakuje wybranej marki!");
            return;
        }

        let formData = new FormData();
        formData.append("Name", modelName);
        formData.append("BrandId", brandId);

        $.ajax({
            url: "/Admin/CreateModel",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                alert("Dodano pomyślnie!");
                $('#addModelModal').modal('hide');

                let modelSelect = $('#CarModelId');
                modelSelect.append(`<option value="${response.id}" selected>${response.name}</option>`);
                // 🔽 Zachowaj pierwszą opcję ("Wybierz model") i posortuj resztę
                let firstOption = modelSelect.find('option').first(); // "Wybierz model"
                let otherOptions = modelSelect.find('option:not(:first)');

                // 🔽 Sortowanie naturalne (alfabetyczne + numeryczne)
                otherOptions.sort(function (a, b) {
                    return $(a).text().localeCompare($(b).text(), 'pl', { sensitivity: 'base', numeric: true });
                });

                // 🔽 Wyczyść select, wstaw z powrotem pierwszą opcję, a potem posortowane
                modelSelect.empty().append(firstOption).append(otherOptions);

                // 🔽 Ustaw nowo dodany model jako wybrany
                modelSelect.val(response.id).trigger('change');

                $('#selectedCarModelId').val(response.id);
                $("#newModelName").val('');
                $('#CarModelId').prop('disabled', false);
                $('#addModelBtn').prop('disabled', false);

                $('#VersionId')
                    .empty()
                    .append('<option value="" disabled selected>Wybierz generację</option>')
                    .prop('disabled', false);

                $('#addVersionBtn').prop('disabled', false);
            },
            error: function (xhr) {
                if (xhr.status === 400) {
                    alert("Model już istnieje");
                } else {
                    alert("Błąd dodawania modelu");
                }
            }
        });
    }

    // -- Funkcja ładowania wersji dla wybranego modelu --
    async function loadVersionsForModel(modelId, selectedVersionId = null) {
        try {
            const response = await $.ajax({
                url: '/Admin/GetVersionsByModel',
                data: { modelId },
                type: 'GET'
            });
            const versionDropdown = $('#VersionId');
            versionDropdown.empty();
            versionDropdown.append($('<option>', {
                value: '',
                text: 'Wybierz generację',
                disabled: true,
                selected: !selectedVersionId
            }));
            response.sort((a, b) => a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' }))
                .forEach(v => versionDropdown.append($('<option>', {
                    value: v.id,
                    text: v.name,
                    selected: selectedVersionId && v.id == selectedVersionId
                })));
            versionDropdown.prop('disabled', false);
            $('#addVersionBtn').prop('disabled', false);
        } catch (e) {
            console.error(e);
            alert("Błąd przy ładowaniu generacji.");
        }
    }

    // --- Aktualizacja tytułu przy zmianie modelu, wersji lub roku ---
    function updateTitle() {
        clearTimeout(window.updateTitleTimeout);
        window.updateTitleTimeout = setTimeout(() => {
            const brandOption = $("#BrandId option:selected");
            const modelOption = $("#CarModelId option:selected");
            const versionOption = $("#VersionId option:selected");

            // Pobierz tylko, jeśli nie są puste / "0"
            const brand = brandOption.val() && brandOption.val() !== "0" ? brandOption.text() : "";
            const model = modelOption.val() && modelOption.val() !== "0" ? modelOption.text() : "";
            const version = versionOption.val() && versionOption.val() !== "0" ? versionOption.text() : "";
            const year = $("#Car_Year").val() || "";

            // Ustaw tytuł
            const newTitle = [brand, model, version, year].filter(Boolean).join(" ");
            $("#Title").val(newTitle);

            // Walidacja tytułu — jeśli pole nie jest puste, usuń błąd
            if (typeof setError === "function") {
                if (newTitle && newTitle.trim().length > 0) {
                    setError('title', '');
                    $('#titleError').text('');
                } else {
                    setError('title', 'Podaj tytuł ogłoszenia');
                    $('#titleError').text('Podaj tytuł ogłoszenia');
                }
            }

            // Odśwież globalne błędy
            if (typeof renderGlobalErrors === "function") renderGlobalErrors();
        }, 300);
    }

    // --- Zmiana wersji ---
    $("#VersionId").on("change input", updateTitle);

    // Dodawanie nowej generacji
    window.addNewVersion = function () {
        let versionName = $("#newVersionName").val();
        let modelId = $("#CarModelId").val();

        if (!versionName || !modelId) {
            alert("Podaj nazwę generacji!");
            return;
        }

        let formData = new FormData();
        formData.append("Name", versionName);
        formData.append("CarModelId", modelId);

        $.ajax({
            url: "/Admin/CreateVersion",
            type: "POST",
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                alert("Dodano pomyślnie!");
                $('#addVersionModal').modal('hide');

                let versionSelect = $('#VersionId');
                versionSelect.append(`<option value="${response.id}" selected>${response.name}</option>`);
                // 🔽 Zachowaj pierwszą opcję ("Wybierz model") i posortuj resztę
                let firstOption = versionSelect.find('option').first(); // "Wybierz wersje"
                let otherOptions = versionSelect.find('option:not(:first)');

                // 🔽 Sortowanie naturalne (alfabetyczne + numeryczne)
                otherOptions.sort(function (a, b) {
                    return $(a).text().localeCompare($(b).text(), 'pl', { sensitivity: 'base', numeric: true });
                });

                // 🔽 Wyczyść select, wstaw z powrotem pierwszą opcję, a potem posortowane
                versionSelect.empty().append(firstOption).append(otherOptions);

                // 🔽 Ustaw nowo dodany model jako wybrany
                versionSelect.val(response.id).trigger('change');

                $('#SelectedCarVersionId').val(response.id);
                $("#newVersionName").val('');
                $('#VersionId').prop('disabled', false);
                $('#addVersionBtn').prop('disabled', false);
            },
            error: function (xhr) {
                if (xhr.status === 400) {
                    alert("Generacja już istnieje");
                } else {
                    alert("Błąd dodawania generacji");
                }
            }
        });
    }

    // --- Walidacja VIN ---
    $('#Car_VIN').on('input change', function () {
        const vinPattern = /^(?!.*\s)[A-HJ-NPR-Z0-9]+$/; // tylko dozwolone znaki, bez długości
        const val = $(this).val() ? $(this).val().trim().toUpperCase() : '';
        let msg = '';

        if (val === '') {
            // VIN jest opcjonalny
            msg = '';
        }
        else if (!vinPattern.test(val)) {
            // zawiera niedozwolone znaki
            msg = "VIN zawiera niedozwolone znaki (bez I, O, Q).";
        }
        else if (val.length < 17) {
            // za krótki
            msg = "VIN musi mieć dokładnie 17 znaków.";
        }
        else if (val.length > 17) {
            // za długi
            msg = "VIN może mieć maksymalnie 17 znaków.";
        }
        else {
            // poprawny
            msg = '';
        }

        $('#vinError').text(msg);
        setError('vin', msg);
    });

    // --- Walidacja pojemności ---
    $('#Car_CubicCapacity').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj pojemność silnika";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość pojemności";
        else if (val < 100) msg = "Pojemność musi być większa niż 100 cm³";
        else if (val > 8000) msg = "Pojemność musi być mniejsza niż 8000 cm³";

        $('#cubicCapacityError').text(msg);
        setError('cubic', msg);
    });

    // --- Walidacja mocy ---
    $('#Car_EnginePower').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj moc silnika";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość mocy";
        else if (val < 20) msg = "Moc musi być większa niż 20 KM";
        else if (val > 2000) msg = "Moc musi być mniejsza niż 2000 KM";

        $('#enginePowerError').text(msg);
        setError('power', msg);
    });

    // --- Walidacja przebiegu ---
    $('#Car_Mileage').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj przebieg samochodu";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość przebiegu";
        else if (val < 1) msg = "Przebieg musi być większy od 0";
        else if (val > 2000000) msg = "Przebieg musi być mniejszy niż 2 000 000 km";

        $('#mileageError').text(msg);
        setError('mileage', msg);
    });

    // --- Walidacja liczby drzwi ---
    $('#Car_Door').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj liczbę drzwi";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość liczby drzwi";
        else if (val < 1) msg = "Liczba drzwi nie może być mniejsza od 1";
        else if (val > 5) msg = "Liczba drzwi musi być mniejsza niż 6";

        $('#doorError').text(msg);
        setError('door', msg);
    });

    // --- Walidacja liczby miejsc ---
    $('#Car_Seat').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj liczbę miejsc";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość liczby miejsc";
        else if (val < 1) msg = "Liczba miejsc nie może być mniejsza od 1";
        else if (val > 10) msg = "Liczba miejsc musi być mniejsza niż 10";

        $('#seatError').text(msg);
        setError('seat', msg);
    });

    // --- Walidacja roku ---
    $('#Car_Year').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        const currentYear = new Date().getFullYear();
        let msg = '';

        if (valRaw === '') msg = "Podaj rok produkcji";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość roku";
        else if (val < 1900) msg = "Rok produkcji musi być większy niż 1900";
        else if (val > currentYear) msg = "Rok produkcji nie może być większy niż obecny rok";

        $('#yearError').text(msg);
        setError('year', msg);
        updateTitle();
    });

    // --- Walidacja starej ceny ---
    $('#Car_OldPrice').on('input change', function () {
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "Podaj cenę";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość ceny";
        else if (val < 1) msg = "Cena musi być większa niż 0 PLN";
        else if (val > 999999) msg = "Cena nie może być większa niż 999 999 PLN";

        $('#oldPriceError').text(msg);
        setError('price', msg);
    });

    // --- Walidacja ceny w promocji ---
    $('#Car_NewPrice').on('input change', function () {
        const valOldRaw = $('#Car_OldPrice').val().trim(); // <-- poprawione
        const valOld = parseInt(valOldRaw, 10);
        const valRaw = $(this).val().trim();
        const val = parseInt(valRaw, 10);
        let msg = '';

        if (valRaw === '') msg = "";
        else if (Number.isNaN(val)) msg = "Nieprawidłowa wartość ceny";
        else if (val < 1) msg = "Cena musi być większa niż 0 PLN";
        else if (!Number.isNaN(valOld) && val > valOld) msg = "Cena w promocji nie może być większa od starej ceny";
        else if (val > 999999) msg = "Cena nie może być większa niż 999 999 PLN";

        $('#newPriceError').text(msg);
        setError('newPrice', msg);
      
    });

    // --- Walidacja tytułu ---
    $('#Title, input[name="Car.Title"]').on('input change', function () {
        const val = $(this).val() ? $(this).val().trim() : '';
        let msg = '';

        if (val === '') msg = "Podaj tytuł ogłoszenia";
        else if (val.length > 100) msg = "Tytuł nie może przekraczać 100 znaków";

        $('#titleError').text(msg);
        setError('title', msg);
    });

    // --- Walidacja daty przeglądu ---
    $('#NextTechnicalBad').on('change input', function () {
        const val = $(this).val();
        let msg = '';

        if (!val) {
            // pole jest opcjonalne, więc brak daty = brak błędu
            msg = '';
        } else {
            const today = new Date();
            const selectedDate = new Date(val);
            const maxDate = new Date();
            // Ustaw dzień dzisiejszy bez godzin, by porównanie było precyzyjne
            today.setHours(0, 0, 0, 0);
            selectedDate.setHours(0, 0, 0, 0);
            maxDate.setFullYear(today.getFullYear() + 3); // maks. 3 lata do przodu
            if (selectedDate < today) {
                msg = "Data przeglądu nie może być z przeszłości";
            }
            else if (selectedDate > maxDate) {
                msg = "Data przeglądu nie może być dalej niż 3 lata od dziś";
            }
            else {
                msg = '';
            }
        }

        $('#nextTechnicalError').text(msg);
        setError('nextTechnical', msg);
    });

    // --- Walidacja daty OC ---
    $('#NextOcBad').on('change input', function () {
        const val = $(this).val();
        let msg = '';

        if (!val) {
            // pole jest opcjonalne, więc brak daty = brak błędu
            msg = '';
        } else {
            const today = new Date();
            const selectedDate = new Date(val);
            const maxDate = new Date();
            // Ustaw dzień dzisiejszy bez godzin, by porównanie było precyzyjne
            today.setHours(0, 0, 0, 0);
            selectedDate.setHours(0, 0, 0, 0);
            maxDate.setFullYear(today.getFullYear() + 3); // maks. 3 lata do przodu
            if (selectedDate < today) {
                msg = "Data ubezpieczenia nie może być z przeszłości";
            }
            else if (selectedDate > maxDate) {
                msg = "Data ubezpieczenia nie może być dalej niż 3 lata od dziś";
            }
            else {
                msg = '';
            }
        }

        $('#nextOcError').text(msg);
        setError('nextOc', msg);
    });

    // --- Walidacja wyposażenia ---
    $('input[name="SelectedEquipmentIds"]').on('change', function () {
        const checkedCount = $('input[name="SelectedEquipmentIds"]:checked').length;
        let msg = '';

        if (checkedCount === 0) {
            msg = "Wybierz przynajmniej jedno wyposażenie";
        }

        $('#equipmentError').text(msg); // lokalny błąd
        setError('equipment', msg);     // globalny div błędów
    });


    // --- Walidacja zdjęć ---
    let photoFiles = []; // nowe zdjęcia z input
    let mainIndex = 0;

    // --- Obsługa dodawania zdjęć ---
    $('#carPhotos').on('change', function () {
        const newFiles = Array.from(this.files);

        // Dodaj nowe pliki do listy
        photoFiles = photoFiles.concat(newFiles);

        // Reset input, by można było ponownie wybrać te same pliki
        this.value = '';

        // Renderuj tylko nowo dodane zdjęcia
        renderPhotoPreviewInitial(newFiles);

        // Ustaw główne zdjęcie, jeśli nie ma
        if (photoFiles.length > 0 && mainIndex >= photoFiles.length) mainIndex = 0;
        setTimeout(() => setMainPhoto(mainIndex), 100);

        validatePhotos();
    });

    // --- Render tylko nowych zdjęć ---
    function renderPhotoPreviewInitial(files) {
        const preview = $('#photoPreview');

        files.forEach((file, i) => {
            if (!file.type.startsWith('image/')) return;

            const index = photoFiles.indexOf(file);
            const reader = new FileReader();
            reader.onload = function (e) {
                const container = createPhotoContainer(e.target.result, index);
                preview.append(container);
            }
            reader.readAsDataURL(file);
        });
    }

    // --- Tworzenie miniaturki ---
    function createPhotoContainer(src, index) {
        const container = $('<div>')
            .addClass('photo-container')
            .attr('data-index', index)
            .css({
                position: 'relative',
                display: 'inline-block',
                marginRight: '10px',
                marginBottom: '10px'
            });

        const img = $('<img>')
            .attr('src', src)
            .addClass('preview-img')
            .attr('data-index', index)
            .css({
                width: '100px',
                height: '100px',
                objectFit: 'cover',
                border: index === mainIndex ? '3px solid green' : '2px solid #ddd',
                borderRadius: '5px',
                cursor: 'pointer'
            })
            .on('click', function () {
                const idx = parseInt($(this).attr('data-index'));
                setMainPhoto(idx);
            });

        const label = $('<span>')
            .addClass('main-label')
            .text('Główne')
            .css({
                position: 'absolute',
                top: '5px',
                left: '5px',
                background: 'green',
                color: '#fff',
                padding: '2px 5px',
                fontSize: '12px',
                borderRadius: '3px',
                display: index === mainIndex ? 'block' : 'none'
            });

        const removeBtn = $('<span>')
            .html('&times;')
            .css({
                position: 'absolute',
                top: '2px',
                right: '2px',
                background: 'rgba(255,0,0,0.8)',
                color: '#fff',
                borderRadius: '50%',
                width: '18px',
                height: '18px',
                lineHeight: '18px',
                textAlign: 'center',
                cursor: 'pointer',
                fontWeight: 'bold',
                fontSize: '14px'
            })
            .on('click', function (e) {
                e.stopPropagation();
                const idx = parseInt($(this).siblings('img').attr('data-index'));
                removePhoto(idx);
            });

        container.append(img).append(label).append(removeBtn);
        return container;
    }

    // --- Ustawienie zdjęcia głównego ---
    function setMainPhoto(newIndex) {
        mainIndex = newIndex;
        $('#mainPhotoIndex').val(mainIndex);

        $('#photoPreview .preview-img').each(function () {
            const idx = parseInt($(this).attr('data-index'));
            const isMain = idx === mainIndex;
            $(this).css('border', isMain ? '3px solid green' : '2px solid #ddd');
            $(this).siblings('.main-label').css('display', isMain ? 'block' : 'none');
        });
    }

    // --- Usuwanie zdjęcia bez przeładowania ---
    function removePhoto(indexToRemove) {
        // Usuń plik z tablicy
        photoFiles.splice(indexToRemove, 1);

        // Usuń element z DOM
        $(`#photoPreview .photo-container[data-index="${indexToRemove}"]`).remove();

        // Zaktualizuj indeksy pozostałych elementów w DOM
        $('#photoPreview .photo-container').each(function (i) {
            $(this).attr('data-index', i);
            $(this).find('.preview-img').attr('data-index', i);
        });

        // Przesuń indeks głównego zdjęcia w razie potrzeby
        if (mainIndex === indexToRemove) mainIndex = 0;
        else if (mainIndex > indexToRemove) mainIndex--;

        setMainPhoto(mainIndex);
        validatePhotos();
    }

    // --- Walidacja zdjęć ---
    function validatePhotos() {
        let msg = '';
        if (photoFiles.length === 0) msg = "Dodaj przynajmniej jedno zdjęcie.";
        else if (photoFiles.length > 30) msg = "Nie można dodać więcej niż 30 zdjęć.";

        $('#photoError').text(msg);
        setError('photos', msg);
    }

    //Wyszukiwarka wyposażenia
    const searchInput = document.getElementById('equipmentSearch');
    searchInput.addEventListener('input', () => {
        const query = searchInput.value.toLowerCase();
        const items = document.querySelectorAll('.equipment-item');

        items.forEach(item => {
            const label = item.querySelector('label').innerText.toLowerCase();
            if (label.includes(query)) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });
    });


    if (isEditMode == false) {
        setError('brand', 'Wybierz markę samochodu');
        setError('model', 'Wybierz model samochodu');
        setError('title', 'Podaj tytuł ogłoszenia');
        setError('cubic', 'Podaj pojemność silnika');
        setError('power', 'Podaj moc silnika');
        setError('year', 'Podaj rok produkcji');
        setError('mileage', 'Podaj przebieg samochodu');
        setError('door', 'Podaj liczbę drzwi');
        setError('seat', 'Podaj liczbę miejsc');
        setError('price', 'Podaj cenę');
        setError('photos', 'Musisz dodać przynajmniej jedno zdjęcie');
        setError('equipment', 'Wybierz przynajmniej jedno wyposażenie');
    }
    else {
        const brandDropdown = document.getElementById("Car_BrandId");
        const modelDropdown = document.getElementById("CarModelId");
        const versionDropdown = document.getElementById("VersionId");

        const addModelBtn = document.getElementById("addModelBtn");
        const addVersionBtn = document.getElementById("addVersionBtn");

        if (brandDropdown) brandDropdown.removeAttribute("disabled");
        if (modelDropdown) modelDropdown.removeAttribute("disabled");
        if (versionDropdown) versionDropdown.removeAttribute("disabled");

        if (addModelBtn) addModelBtn.style.display = "inline-block";
        if (addVersionBtn) addVersionBtn.style.display = "inline-block";

        const existingPhotosEl = document.getElementById("existingPhotosData");
        let existingPhotos = [];

        if (existingPhotosEl) {
            try {
                existingPhotos = JSON.parse(existingPhotosEl.dataset.photos);
            } catch (e) {
                console.error("Błąd parsowania existingPhotos:", e);
            }
        }
        console.log("existingPhotos:", existingPhotos);
        if (!existingPhotos || existingPhotos.length === 0) return;

        const preview = $('#photoPreview');

        existingPhotos.forEach((photo, i) => {
            photoFiles.push(null); // zachowanie struktury
            const container = createPhotoContainer(photo.Src, i);
            preview.append(container);
            if (photo.isMain) mainIndex = i;
        });

        // ✅ te dwie funkcje powinny być wywołane po pętli, nie w niej
        setMainPhoto(mainIndex);
        validatePhotos();
    }

});

