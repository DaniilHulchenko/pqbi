import { AppConsts } from '@shared/AppConsts';
import { Component, Injector, ViewEncapsulation, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomParametersServiceProxy, CustomParameterDto } from '@shared/service-proxies/service-proxies';
import { NotifyService } from 'abp-ng2-module';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TokenAuthServiceProxy } from '@shared/service-proxies/service-proxies';
import { CreateOrEditCustomParameterModalComponent } from './create-or-edit-customParameter-modal.component';

import { ViewCustomParameterModalComponent } from './view-customParameter-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Table } from 'primeng/table';
import { Paginator } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { filter as _filter } from 'lodash-es';
import { DateTime } from 'luxon';

import { DateTimeService } from '@app/shared/common/timing/date-time.service';

import { HttpClient } from '@angular/common/http';
import { FileUpload } from 'primeng/fileupload';
import { finalize } from 'rxjs';

@Component({
    templateUrl: './customParameters.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
})
export class CustomParametersComponent extends AppComponentBase {
    @ViewChild('createOrEditCustomParameterModal', { static: true })
    createOrEditCustomParameterModal: CreateOrEditCustomParameterModalComponent;
    @ViewChild('viewCustomParameterModal', { static: true })
    viewCustomParameterModal: ViewCustomParameterModalComponent;

    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    @ViewChild('ExcelFileUpload', { static: false }) excelFileUpload: FileUpload;

    advancedFiltersAreShown = false;
    filterText = '';
    nameFilter = '';
    aggregationFunctionFilter = '';
    stdpqsParametersListFilter = '';
    typeFilter = '';

    uploadUrl: string;

    constructor(
        injector: Injector,
        private _customParametersServiceProxy: CustomParametersServiceProxy,
        private _notifyService: NotifyService,
        private _tokenAuth: TokenAuthServiceProxy,
        private _activatedRoute: ActivatedRoute,
        private _fileDownloadService: FileDownloadService,
        private _dateTimeService: DateTimeService,
        private _httpClient: HttpClient,
    ) {
        super(injector);

        this.uploadUrl = AppConsts.remoteServiceBaseUrl + '/CustomParameters/ImportFromExcel';
    }

    getCustomParameters(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._customParametersServiceProxy
            .getAll(
                this.filterText,
                this.nameFilter,
                this.aggregationFunctionFilter,
                this.typeFilter,
                undefined, // maxResolutionInSecondsFilter
                undefined, // minResolutionInSecondsFilter
                undefined, 
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event),
            )
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        this.paginator.changePage(this.paginator.getPage());
    }

    createCustomParameter(): void {
        this.createOrEditCustomParameterModal.show();
    }

    deleteCustomParameter(customParameter: CustomParameterDto): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._customParametersServiceProxy.delete(customParameter.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    exportToExcel(): void {
        this._customParametersServiceProxy
            .getCustomParametersToExcel(
                this.filterText,
                this.nameFilter,
                this.aggregationFunctionFilter,
                this.typeFilter,
                undefined,
                undefined,
                undefined
            )
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    uploadExcel(data: { files: File }): void {
        const formData: FormData = new FormData();
        const file = data.files[0];
        formData.append('file', file, file.name);

        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(finalize(() => this.excelFileUpload.clear()))
            .subscribe((response) => {
                if (response.success) {
                    this.notify.success(this.l('ImportCustomParametersProcessStart'));
                } else if (response.error != null) {
                    this.notify.error(this.l('ImportCustomParametersUploadFailed'));
                }
            });
    }

    onUploadExcelError(): void {
        this.notify.error(this.l('ImportCustomParametersUploadFailed'));
    }

    resetFilters(): void {
        this.filterText = '';
        this.nameFilter = '';
        this.aggregationFunctionFilter = '';
        this.stdpqsParametersListFilter = '';
        this.typeFilter = '';

        this.getCustomParameters();
    }
}
