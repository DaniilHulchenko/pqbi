import { Injectable } from '@angular/core';
import {
    CreateOrEditDefaultValueDto,
    CreateOrEditFileInfoDto,
    FileInfosServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { map, Observable, of, switchMap } from 'rxjs';
import { DefaultValuesService } from './default-values-service.service';
import { CardIcon } from '../interfaces/card-icon';

@Injectable({
    providedIn: 'root',
})
export class CardIconService {
    private readonly defaultIconSettingName = 'UI.CardWidget.DefaultIconId';

    constructor(
        private fileInfosService: FileInfosServiceProxy,
        private defaultValuesService: DefaultValuesService,
    ) {}

    getAvailableIcons(): Observable<CardIcon[]> {
        return this.fileInfosService.getAll(undefined, 'id desc', 0, 50).pipe(
            map((result) =>
                result.items?.map((item) => ({
                    id: item.fileInfo.id,
                    name: item.fileInfo.name ?? '',
                    content: item.fileInfo.content ?? '',
                })) ?? [],
            ),
        );
    }

    getIconById(id: number): Observable<CardIcon | null> {
        if (!id) {
            return of(null);
        }

        return this.fileInfosService.getFileInfoForView(id).pipe(
            map((result) =>
                result?.fileInfo
                    ? { id: result.fileInfo.id, name: result.fileInfo.name ?? '', content: result.fileInfo.content ?? '' }
                    : null,
            ),
        );
    }

    uploadIcon(file: File): Observable<CardIcon> {
        return this.readFileContent(file).pipe(
            switchMap((content) => {
                const fileInfoDto = new CreateOrEditFileInfoDto({
                    id: undefined,
                    name: file.name,
                    content,
                });

                return this.fileInfosService
                    .createOrEdit(fileInfoDto)
                    .pipe(switchMap(() => this.fetchLatestIcon()));
            }),
        );
    }

    getDefaultIconId(): Observable<number | null> {
        return this.defaultValuesService.getValue(this.defaultIconSettingName).pipe(
            map((value) => (value ? +value : null)),
        );
    }

    setDefaultIcon(id: number): Observable<void> {
        return this.defaultValuesService.createOrEdit(
            new CreateOrEditDefaultValueDto({
                id: null,
                name: this.defaultIconSettingName,
                value: id?.toString(),
            }),
        );
    }

    private fetchLatestIcon(): Observable<CardIcon> {
        return this.fileInfosService.getAll(undefined, 'id desc', 0, 1).pipe(
            map((result) => {
                const icon = result.items?.[0]?.fileInfo;
                return {
                    id: icon?.id ?? 0,
                    name: icon?.name ?? '',
                    content: icon?.content ?? '',
                } as CardIcon;
            }),
        );
    }

    private readFileContent(file: File): Observable<string> {
        return new Observable((observer) => {
            const reader = new FileReader();
            reader.onload = () => {
                observer.next(reader.result as string);
                observer.complete();
            };
            reader.onerror = (error) => observer.error(error);
            reader.readAsDataURL(file);
        });
    }
}

