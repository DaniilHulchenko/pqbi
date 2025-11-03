import { Injectable } from "@angular/core";
import { CustomParameterDto, CustomParametersServiceProxy, PagedResultDtoOfGetCustomParameterForViewDto } from "@shared/service-proxies/service-proxies";
import { Observable, of, tap, map, shareReplay } from "rxjs";
import { ConfigurationVersionService } from "./configuration-version-service.service";
import { CacheStorageService } from "./cache-storage.service";

@Injectable({
    providedIn: 'root',
})
export class CustomParameterService {
    private readonly CACHE_KEY = 'custom_parameters';
    private _pendingRequest: Observable<CustomParameterDto[]> | null = null;

    constructor(
        private customParameterServiceProxy: CustomParametersServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    ){
        this.configurationVersionService.getVersionChanged$().subscribe(() => {
            this.clearCache();
        });
    }

    private clearCache(): void {
        this.cacheStorage.remove(this.CACHE_KEY);
        this._pendingRequest = null;
    }

    getAll(
        typeFilter: string[] | undefined = undefined,
    ): Observable<CustomParameterDto[]> {
        const cachedParameters = this.cacheStorage.get<CustomParameterDto[]>(this.CACHE_KEY);
        if(cachedParameters && cachedParameters.length > 0){
            const filtered = typeFilter 
                ? cachedParameters.filter(param => typeFilter.includes(param.type))
                : cachedParameters;
            
            return of(filtered);
        }

        if (this._pendingRequest) {
            return this._pendingRequest.pipe(
                map((parameters: CustomParameterDto[]) => 
                    typeFilter 
                        ? parameters.filter(param => typeFilter.includes(param.type))
                        : parameters
                )
            );
        }

        this._pendingRequest = this.customParameterServiceProxy
            .getAll(
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined, 
                undefined,
                0,
                10000
            )
            .pipe(
                map((result: PagedResultDtoOfGetCustomParameterForViewDto) => 
                    result.items?.map(item => item.customParameter) || []
                ),
                tap((parameters: CustomParameterDto[]) => {
                    this.cacheStorage.set(this.CACHE_KEY, parameters);
                    this._pendingRequest = null;
                }),
                shareReplay({ bufferSize: 1, refCount: false })
            );

        return this._pendingRequest.pipe(
            map((parameters: CustomParameterDto[]) => 
                typeFilter 
                    ? parameters.filter(param => typeFilter.includes(param.type))
                    : parameters
            )
        );
    }

    getById(id: number): Observable<CustomParameterDto> {
        const cachedParameters = this.cacheStorage.get<CustomParameterDto[]>(this.CACHE_KEY);
        if (cachedParameters) {
            const parameter = cachedParameters.find(p => p.id === id);
            if (parameter) {
                return of(parameter);
            }
        }

        return this.customParameterServiceProxy
            .getCustomParameterForView(id)
            .pipe(
                map(response => response.customParameter)
            );
    }
}
